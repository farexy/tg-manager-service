using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using TG.Core.App.Services;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.Options;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Application.Background
{
    public class NodePortsManager : BackgroundService
    {
        private readonly PortsManagerSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IKubernetes _kubernetes;
        private readonly ILogger<LoadBalancerManager> _logger;
        private readonly PortsRange _portsRange;

        public NodePortsManager(IServiceProvider serviceProvider, IOptions<PortsManagerSettings> settings,
            IDateTimeProvider dateTimeProvider, IKubernetes kubernetes, ILogger<LoadBalancerManager> logger,
            IOptions<PortsRange> portsRange)
        {
            _serviceProvider = serviceProvider;
            _dateTimeProvider = dateTimeProvider;
            _kubernetes = kubernetes;
            _logger = logger;
            _portsRange = portsRange.Value;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ProcessReservedPortsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    var terminatingTime =
                        _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromHours(_settings.TerminatingIntervalHours));
                    var inactivePorts = await dbContext.NodePorts
                        .Where(lb => lb.State == NodePortState.Active && lb.LastUpdate <= terminatingTime)
                        .ToListAsync(stoppingToken);

                    inactivePorts.ForEach(nodePort =>
                    {
                        nodePort.State = NodePortState.Terminating;
                        nodePort.LastUpdate = _dateTimeProvider.UtcNow;
                    });
                    await dbContext.SaveChangesAsync(stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingTimeoutSec), stoppingToken);

                    var disposingLbs = await dbContext.NodePorts
                        .Where(lb => lb.State == NodePortState.Disposing)
                        .ToListAsync(stoppingToken);
                    await Task.WhenAll(
                        disposingLbs.Select(async nodePort =>
                        {
                            try
                            {
                                await _kubernetes.ReadNamespacedServiceWithHttpMessagesAsync(nodePort.SvcName,
                                    K8sNamespaces.Tg, cancellationToken: stoppingToken);
                            }
                            catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode == HttpStatusCode.NotFound)
                            {
                                nodePort.State = NodePortState.Inactive;
                                nodePort.SvcName = null;
                                nodePort.LastUpdate = _dateTimeProvider.UtcNow;
                            }
                        })
                    );

                    await dbContext.SaveChangesAsync(stoppingToken);

                    var terminatingLbs = await dbContext.NodePorts
                        .Where(lb => lb.State == NodePortState.Terminating)
                        .ToListAsync(stoppingToken);
                    await Task.WhenAll(
                        terminatingLbs.Select(nodePort =>
                        {
                            nodePort.State = NodePortState.Disposing;
                            nodePort.LastUpdate = _dateTimeProvider.UtcNow;
                            return _kubernetes.DeleteNamespacedServiceAsync(nodePort.SvcName, K8sNamespaces.Tg,
                                cancellationToken: stoppingToken);
                        })
                    );

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected exception");
                }
            }
        }

        private async Task ProcessReservedPortsAsync(CancellationToken stoppingToken)
        {
            const string nonBattleServerSvcSelector = "type!=bs";

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var services = await _kubernetes.ListNamespacedServiceAsync(
                K8sNamespaces.Tg,
                labelSelector: nonBattleServerSvcSelector,
                cancellationToken: stoppingToken);
            var reservedServices = services.Items.Where(s =>
                    s.Spec.Ports.Any(p => p.NodePort.HasValue && p.NodePort >= _portsRange.Min && p.NodePort <= _portsRange.Max))
                .ToList();

            var existingReservedPorts = await dbContext.NodePorts
                .Where(p => p.State == NodePortState.Reserved)
                .ToListAsync(stoppingToken);
            foreach (var reservedSvc in reservedServices)
            {
                var port = reservedSvc.Spec.Ports.First().NodePort!.Value;
                if (existingReservedPorts.All(record => record.Port != port))
                {
                    await dbContext.AddAsync(new NodePort
                    {
                        Port = port,
                        LastUpdate = _dateTimeProvider.UtcNow,
                        State = NodePortState.Reserved,
                        SvcName = reservedSvc.Metadata.Name,
                    }, stoppingToken);
                }
            }
                    
            dbContext.RemoveRange(existingReservedPorts
                .Where(p => reservedServices.All(svc => svc.Metadata.Name != p.SvcName)));

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}