using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using MediatR;
using TG.Core.App.Services;
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

        public BattleStateChangedEventHandler(ApplicationDbContext dbContext, IKubernetes kubernetes,
            IQueueProducer<BattleEndedMessage> queueProducer, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _queueProducer = queueProducer;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Handle(BattleStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var lb = notification.BattleServer.LoadBalancer!;
            if (notification.State is BattleServerState.Ready)
            {
                lb.PublicIp = await TryGetLoadBalancerIpWithRetryAsync(lb.SvcName, 0, cancellationToken);
                lb.State = LoadBalancerState.Busy;
                lb.LastUpdate = _dateTimeProvider.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            if (notification.State is BattleServerState.Ended)
            {
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

        private async Task<string?> TryGetLoadBalancerIpWithRetryAsync(string svcName, int retryCount, CancellationToken cancellationToken)
        {
            const int failRetry = 15;
            const int retryMs = 3000;
            if (retryCount >= failRetry)
            {
                throw new ApplicationException("Can not retrieve load balancer ip. Service: " + svcName);
            }
            var service = await _kubernetes.ReadNamespacedServiceWithHttpMessagesAsync(
                svcName, K8sNamespaces.Tg, cancellationToken: cancellationToken);

            var ip = service.Body.Status.LoadBalancer.Ingress?.FirstOrDefault()?.Ip;
            if (ip is null)
            {
                await Task.Delay(retryMs, cancellationToken);
                return await TryGetLoadBalancerIpWithRetryAsync(svcName, ++retryCount, cancellationToken);
            }

            return ip;
        }
    }
}