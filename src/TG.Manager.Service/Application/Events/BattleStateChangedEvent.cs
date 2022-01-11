using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using MediatR;
using Microsoft.Extensions.Logging;
using TG.Core.App.Services;
using TG.Core.Files;
using TG.Manager.Service.Config;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Events
{
    public record BattleStateChangedEvent(BattleServerState State, BattleServer BattleServer) : INotification;
    
    public class BattleStateChangedEventHandler : INotificationHandler<BattleStateChangedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IKubernetes _kubernetes;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IStorageContainerClient _storage;
        private readonly ILogger<BattleStateChangedEventHandler> _logger;
        private readonly INodeProvider _nodeProvider;

        public BattleStateChangedEventHandler(ApplicationDbContext dbContext, IKubernetes kubernetes,
            IDateTimeProvider dateTimeProvider, IStorageContainerClient storage,
            ILogger<BattleStateChangedEventHandler> logger, INodeProvider nodeProvider)
        {
            _dbContext = dbContext;
            _kubernetes = kubernetes;
            _dateTimeProvider = dateTimeProvider;
            _storage = storage;
            _logger = logger;
            _nodeProvider = nodeProvider;
        }

        public async Task Handle(BattleStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var server = notification.BattleServer;
            var nodePort = notification.BattleServer.NodePort!;
            if (notification.State is BattleServerState.Ready)
            {
                server.NodeIp = await _nodeProvider
                    .TryGetNodeIpWithRetryAsync(ParseAppLabel(notification.BattleServer.DeploymentName), cancellationToken);
                nodePort.State = NodePortState.Busy;
                nodePort.LastUpdate = _dateTimeProvider.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            if (notification.State is BattleServerState.Ended)
            {
                await SaveServerLogsAsync(notification.BattleServer, cancellationToken);
                await _kubernetes.DeleteNamespacedDeploymentAsync(notification.BattleServer.DeploymentName,
                    K8sNamespaces.Tg, cancellationToken: cancellationToken);
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

        private static readonly int DeploymentStrLen = "-deployment".Length;

        private static string ParseAppLabel(string deploymentName) => deploymentName[..^DeploymentStrLen];

        private static string LogsFileName(BattleServer server) => $"battles_{DateTime.UtcNow:MM.yyyy}/{server.BattleId}.log";
    }
}