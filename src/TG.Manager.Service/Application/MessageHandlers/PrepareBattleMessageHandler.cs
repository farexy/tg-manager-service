using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TG.Core.ServiceBus;
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

        public PrepareBattleMessageHandler(ApplicationDbContext dbContext, IKubernetes kubernetes,
            IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider, IOptions<PortsRange> portsRange)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
            _portsRange = portsRange.Value;
        }

        public async Task HandleMessage(PrepareBattleMessage message, CancellationToken cancellationToken)
        {
            var port = await _dbContext.BattleServers.MaxAsync(s => s.Port, cancellationToken);
            port = port == default
                ? _portsRange.Min
                : port > _portsRange.Max
                    ? _portsRange.Min
                    : port;
            var yaml = Yaml.LoadAllFromString(await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync(port));
            var deployment = (yaml[0] as V1Deployment)!;
            var service = (yaml[1] as V1Service)!;
            
            var battleServer = new BattleServer
            {
                Port = port,
                BattleId = message.BattleId,
                State = BattleServerState.Initializing,
                DeploymentName = deployment.Metadata.Name,
                SvcName = service.Metadata.Name,
            };

            await _dbContext.BattleServers.AddAsync(battleServer, cancellationToken);

            await Task.WhenAll(
                _dbContext.SaveChangesAsync(cancellationToken),
                _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment,
                    K8sNamespaces.Tg, cancellationToken: cancellationToken),
                _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(service,
                    K8sNamespaces.Tg, cancellationToken: cancellationToken)
            );
        }
    }
}