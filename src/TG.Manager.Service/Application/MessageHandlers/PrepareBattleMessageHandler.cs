using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TG.Core.App.Services;
using TG.Core.ServiceBus;
using TG.Core.ServiceBus.Messages;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.Options;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.MessageHandlers
{
    public class PrepareBattleMessageHandler : IMessageHandler<PrepareBattleMessage>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IKubernetes _kubernetes;
        private readonly IRealtimeServerDeploymentConfigProvider _realtimeServerDeploymentConfigProvider;
        private readonly PortsRange _portsRange;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITestBattlesHelper _testBattlesHelper;

        public PrepareBattleMessageHandler(ApplicationDbContext dbContext, IKubernetes kubernetes, IOptions<PortsRange> portsRange,
            IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider, IDateTimeProvider dateTimeProvider, ITestBattlesHelper testBattlesHelper)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
            _dateTimeProvider = dateTimeProvider;
            _testBattlesHelper = testBattlesHelper;
            _portsRange = portsRange.Value;
        }

        public async Task HandleMessage(PrepareBattleMessage message, CancellationToken cancellationToken)
        {
            if (_testBattlesHelper.IsTestServer(message.BattleId))
            {
                return;
            }

            var nodePort = await AllocatePortAsync(cancellationToken);

            var yaml = Yaml.LoadAllFromString(
                await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync(nodePort.Port, message.BattleId));
            var deployment = (yaml[0] as V1Deployment)!;
            var service = (yaml[1] as V1Service)!;
    
            var battleServer = new BattleServer
            {
                BattleId = message.BattleId,
                State = BattleServerState.Initializing,
                NodePort = nodePort,
                DeploymentName = deployment.Metadata.Name,
                InitializationTime = _dateTimeProvider.UtcNow,
                LastUpdate = _dateTimeProvider.UtcNow,
            };
            await _dbContext.BattleServers.AddAsync(battleServer, cancellationToken);

            Task svcInitialization;
            if (nodePort.State == NodePortState.Active)
            {
                svcInitialization = Task.CompletedTask;
                // not to conflict state with LbManager
                _dbContext.Entry(nodePort).Property(port => port.State).IsModified = true;
            }
            else
            {
                svcInitialization = _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(
                    service, K8sNamespaces.Tg, cancellationToken: cancellationToken);
                nodePort.State = NodePortState.Initializing;
            }
            nodePort.SvcName = service.Metadata.Name;
            nodePort.LastUpdate = _dateTimeProvider.UtcNow;

            var deploymentInitialization = _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(
                deployment,K8sNamespaces.Tg, cancellationToken: cancellationToken);
            await Task.WhenAll(
                _dbContext.SaveChangesAsync(cancellationToken),
                svcInitialization,
                deploymentInitialization
            );
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

            var nodePort = new NodePort
            {
                Port = port,
                State = NodePortState.Initializing,
            };

            await _dbContext.AddAsync(nodePort, cancellationToken);
            return nodePort;
        }
    }
}