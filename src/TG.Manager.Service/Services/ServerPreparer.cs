using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TG.Core.App.Services;
using TG.Core.Db.Postgres.Extensions;
using TG.Manager.Service.Application.Events;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.Options;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Services;

public class ServerPreparer : IServerPreparer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IKubernetes _kubernetes;
    private readonly IRealtimeServerDeploymentConfigProvider _realtimeServerDeploymentConfigProvider;
    private readonly PortsRange _portsRange;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPublisher _publisher;

    public ServerPreparer(ApplicationDbContext dbContext, IKubernetes kubernetes, IOptions<PortsRange> portsRange,
        IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider, IDateTimeProvider dateTimeProvider, IPublisher publisher)
    {
        _dbContext = dbContext;
        _kubernetes = kubernetes;
        _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
        _portsRange = portsRange.Value;
        _dateTimeProvider = dateTimeProvider;
        _publisher = publisher;
    }

    public async Task<BattleServer> PrepareAsync(bool allocate, CancellationToken cancellationToken)
    {
        var nodePort = await AllocatePortAsync(cancellationToken);

        var battleId = Guid.NewGuid();
        var yaml = Yaml.LoadAllFromString(
            await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync(nodePort.Port, battleId));
        var deployment = (yaml[0] as V1Deployment)!;
        var service = (yaml[1] as V1Service)!;

        var battleServer = new BattleServer
        {
            BattleId = battleId,
            State = BattleServerState.Initializing,
            NodePort = nodePort,
            DeploymentName = deployment.Metadata.Name,
            Allocated = allocate,
            InitializationTime = _dateTimeProvider.UtcNow,
            LastUpdate = _dateTimeProvider.UtcNow,
        };
        await _dbContext.BattleServers.AddAsync(battleServer, cancellationToken);

        Task svcInitialization = nodePort.State is NodePortState.Initializing
            ? _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(service, K8sNamespaces.Tg,
                cancellationToken: cancellationToken)
            : Task.CompletedTask;
        nodePort.SvcName = service.Metadata.Name;
        nodePort.LastUpdate = _dateTimeProvider.UtcNow;

        var deploymentInitialization = _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(
            deployment, K8sNamespaces.Tg, cancellationToken: cancellationToken);
        await Task.WhenAll(
            _dbContext.SaveChangesAsync(cancellationToken),
            svcInitialization,
            deploymentInitialization
        );

        await _dbContext.SaveChangesAtomicallyAsync(
            () => Task.WhenAll(svcInitialization, deploymentInitialization),
            async ex =>
            {
                await Task.WhenAll(
                    _kubernetes.DeleteNamespacedDeploymentWithHttpMessagesAsync(battleServer.DeploymentName,
                        K8sNamespaces.Tg, cancellationToken: cancellationToken),
                    nodePort.State is NodePortState.Initializing
                        ? _kubernetes.DeleteNamespacedServiceWithHttpMessagesAsync(nodePort.SvcName,
                            K8sNamespaces.Tg, cancellationToken: cancellationToken)
                        : Task.CompletedTask);

                if (ex is DbUpdateConcurrencyException)
                {
                    await _dbContext.Entry(nodePort).ReloadAsync(cancellationToken);
                }

                nodePort.LastUpdate = _dateTimeProvider.UtcNow;
                nodePort.State = NodePortState.Inactive;
                _dbContext.Remove(battleServer);
                await _dbContext.SaveChangesAsync(cancellationToken);
            });

        return battleServer;
    }
    
    private async Task<NodePort> AllocatePortAsync(CancellationToken cancellationToken)
        {
            var nodePort = await _dbContext.NodePorts
                .OrderByDescending(port => port.State)
                .ThenBy(port => port.Port)
                .FirstOrDefaultAsync(port =>
                    port.State == NodePortState.Active || port.State == NodePortState.Inactive, cancellationToken);

            nodePort ??= await InitNewPortAsync(cancellationToken);

            nodePort.State = nodePort.State is NodePortState.Active 
                ? NodePortState.Busy 
                : NodePortState.Initializing;
            nodePort.LastUpdate = _dateTimeProvider.UtcNow;

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.Entry(nodePort).State = EntityState.Detached;
                return await AllocatePortAsync(cancellationToken);
            }
            catch (DbUpdateException dbEx)
                when (dbEx.InnerException is PostgresException {SqlState: PostgresErrorCodes.UniqueViolation})
            {
                _dbContext.Entry(nodePort).State = EntityState.Detached;
                return await AllocatePortAsync(cancellationToken);
            }

            return nodePort;
        }

        private async Task<NodePort> InitNewPortAsync(CancellationToken cancellationToken)
        {
            int port;
            try
            {
                port = await _dbContext.NodePorts
                    .Where(p => p.Port >= _portsRange.Min)
                    .MaxAsync(s => s.Port, cancellationToken);
                port++;
            }
            catch (InvalidOperationException)
            {
                port = _portsRange.Min;
            }

            if (port > _portsRange.Max)
            {
                await _publisher.Publish(new AllPortsAllocatedEvent(port), cancellationToken);
            }

            var nodePort = new NodePort
            {
                Port = port,
                State = NodePortState.Initializing,
            };

            await _dbContext.AddAsync(nodePort, cancellationToken);
            return nodePort;
        }
}