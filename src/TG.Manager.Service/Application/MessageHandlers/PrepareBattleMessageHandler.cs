using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

        public PrepareBattleMessageHandler(ApplicationDbContext dbContext, IKubernetes kubernetes, IOptions<PortsRange> portsRange,
            IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
            _dateTimeProvider = dateTimeProvider;
            _portsRange = portsRange.Value;
        }

        public async Task HandleMessage(PrepareBattleMessage message, CancellationToken cancellationToken)
        {
            var loadBalancer = await _dbContext.LoadBalancers
                .OrderByDescending(lb => lb.State)
                .FirstOrDefaultAsync(lb => lb.State == LoadBalancerState.Active || lb.State == LoadBalancerState.Inactive, cancellationToken);

            loadBalancer ??= await InitNewLbAsync(cancellationToken);

            var yaml = Yaml.LoadAllFromString(
                await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync(loadBalancer.Port, message.BattleId));
            var deployment = (yaml[0] as V1Deployment)!;
            var service = (yaml[1] as V1Service)!;
    
            var battleServer = new BattleServer
            {
                BattleId = message.BattleId,
                State = BattleServerState.Initializing,
                LoadBalancer = loadBalancer,
                DeploymentName = deployment.Metadata.Name,
                InitializationTime = _dateTimeProvider.UtcNow,
                LastUpdate = _dateTimeProvider.UtcNow,
            };
            await _dbContext.BattleServers.AddAsync(battleServer, cancellationToken);

            var svcInitialization = loadBalancer.State == LoadBalancerState.Active
                ? Task.CompletedTask
                : _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(service, K8sNamespaces.Tg, cancellationToken: cancellationToken);

            loadBalancer.SvcName = service.Metadata.Name;
            loadBalancer.LastUpdate = _dateTimeProvider.UtcNow;

            await Task.WhenAll(
                _dbContext.SaveChangesAsync(cancellationToken),
                _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment,
                    K8sNamespaces.Tg, cancellationToken: cancellationToken),
                svcInitialization
            );
        }

        private async Task<LoadBalancer> InitNewLbAsync(CancellationToken cancellationToken)
        {
            int port;
            try
            {
                port = await _dbContext.LoadBalancers.MaxAsync(s => s.Port, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                port = _portsRange.Min;
            }

            var lb = new LoadBalancer
            {
                Port = port,
                State = LoadBalancerState.Initializing,
            };

            await _dbContext.AddAsync(lb, cancellationToken);
            return lb;
        }
    }
}