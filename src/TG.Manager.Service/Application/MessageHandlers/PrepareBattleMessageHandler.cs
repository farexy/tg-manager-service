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
            var loadBalancer = await _dbContext.LoadBalancers
                .OrderByDescending(lb => lb.State)
                .ThenBy(lb => lb.Port)
                .FirstOrDefaultAsync(lb =>
                    lb.State == LoadBalancerState.Active || lb.State == LoadBalancerState.Inactive, cancellationToken);

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

            var deploymentInitialization = await _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(
                deployment,K8sNamespaces.Tg, cancellationToken: cancellationToken);
            Task svcInitialization;
            if (loadBalancer.State == LoadBalancerState.Active)
            {
                svcInitialization = Task.CompletedTask;
                // not to conflict state with LbManager
                _dbContext.Entry(loadBalancer).Property(lb => lb.State).IsModified = true;
            }
            else
            {
                svcInitialization = _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(
                    service, K8sNamespaces.Tg, cancellationToken: cancellationToken);
                loadBalancer.State = LoadBalancerState.Initializing;
            }
            loadBalancer.SvcName = service.Metadata.Name;
            loadBalancer.LastUpdate = _dateTimeProvider.UtcNow;

            await Task.WhenAll(
                _dbContext.SaveChangesAsync(cancellationToken),
                svcInitialization
            );
        }

        private async Task<LoadBalancer> InitNewLbAsync(CancellationToken cancellationToken)
        {
            int port;
            try
            {
                port = await _dbContext.LoadBalancers
                    .Where(p => p.Port > _portsRange.Min)
                    .MaxAsync(s => s.Port, cancellationToken);
                port++;
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