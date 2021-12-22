using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using MediatR;
using Microsoft.Extensions.Logging;
using TG.Core.App.Services;
using TG.Core.Files;
using TG.Core.ServiceBus;
using TG.Core.ServiceBus.Messages;
using TG.Manager.Service.Config;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Application.Events
{
    public record BattleStateChangedEvent(BattleServerState State, BattleServer BattleServer) : INotification;
    
    public class BattleStateChangedEventHandler : INotificationHandler<BattleStateChangedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IKubernetes _kubernetes;
        private readonly IQueueProducer<BattleEndedMessage> _queueProducer;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IStorageContainerClient _storage;
        private readonly ILogger<BattleStateChangedEventHandler> _logger;

        public BattleStateChangedEventHandler(ApplicationDbContext dbContext, IKubernetes kubernetes,
            IQueueProducer<BattleEndedMessage> queueProducer, IDateTimeProvider dateTimeProvider, IStorageContainerClient storage,
            ILogger<BattleStateChangedEventHandler> logger)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _queueProducer = queueProducer;
            _dateTimeProvider = dateTimeProvider;
            _storage = storage;
            _logger = logger;
        }

        public async Task Handle(BattleStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var lb = notification.BattleServer.LoadBalancer!;
            if (notification.State is BattleServerState.Ready)
            {
                lb.PublicIp = await TryGetLoadBalancerIpWithRetryAsync(notification.BattleServer.DeploymentName, 0, cancellationToken);
                lb.State = NodePortState.Busy;
                lb.LastUpdate = _dateTimeProvider.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            if (notification.State is BattleServerState.Ended)
            {
                await SaveServerLogsAsync(notification.BattleServer, cancellationToken);
                await Task.WhenAll(
                    _kubernetes.DeleteNamespacedDeploymentAsync(notification.BattleServer.DeploymentName,
                        K8sNamespaces.Tg, cancellationToken: cancellationToken),
                    _queueProducer.SendMessageAsync(new BattleEndedMessage
                    {
                        BattleId = notification.BattleServer.BattleId,
                        Reason = BattleEndReason.Finished
                    }));
            }
        }

        private async Task SaveServerLogsAsync(BattleServer server, CancellationToken cancellationToken)
        {
            try
            {
                var pods = await _kubernetes.ListNamespacedPodAsync(K8sNamespaces.Tg,
                    labelSelector: "app=" + ParseAppLabel(server.DeploymentName), cancellationToken: cancellationToken);
                var logs = await _kubernetes.ReadNamespacedPodLogAsync(pods.Items.Single().Metadata.Name,
                    K8sNamespaces.Tg, cancellationToken: cancellationToken);
                await _storage.UploadAsync(logs, LogsFileName(server));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch logs for battle: " + server.BattleId);
            }
        }

        private async Task<string?> TryGetLoadBalancerIpWithRetryAsync(string deploymentName, int retryCount, CancellationToken cancellationToken)
        {
            const int failRetry = 15;
            const int retryMs = 3000;
            if (retryCount >= failRetry)
            {
                throw new ApplicationException("Can not retrieve load balancer ip. Deployment: " + deploymentName);
            }
            var pods = await _kubernetes.ListNamespacedPodAsync(K8sNamespaces.Tg,
                labelSelector: "app=" + ParseAppLabel(deploymentName), cancellationToken: cancellationToken);
            var node = await _kubernetes.ReadNodeAsync(pods.Items.Single().Spec.NodeName,
                cancellationToken: cancellationToken);

            var ip = node.Status.Addresses.FirstOrDefault(a => a.Type == "ExternalIP")?.Address;
            if (ip is null)
            {
                await Task.Delay(retryMs, cancellationToken);
                return await TryGetLoadBalancerIpWithRetryAsync(deploymentName, ++retryCount, cancellationToken);
            }

            return ip;
        }

        private static readonly int DeploymentStrLen = "-deployment".Length;

        private static string ParseAppLabel(string deploymentName) => deploymentName[..^DeploymentStrLen];

        private static string LogsFileName(BattleServer server) => $"battles_{DateTime.UtcNow:MM.yyyy}/{server.BattleId}_({server.LoadBalancerPort}).log";
    }
}