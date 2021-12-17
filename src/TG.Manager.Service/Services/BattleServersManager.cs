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

namespace TG.Manager.Service.Services
{
    public class BattleServersManager : BackgroundService
    {
        private readonly BsManagerSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IKubernetes _kubernetes;
        private readonly ILogger<BattleServersManager> _logger;

        public BattleServersManager(IServiceProvider serviceProvider, IOptions<BsManagerSettings> settings,
            IDateTimeProvider dateTimeProvider, IKubernetes kubernetes, ILogger<BattleServersManager> logger)
        {
            _serviceProvider = serviceProvider;
            _dateTimeProvider = dateTimeProvider;
            _kubernetes = kubernetes;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    var readyInactiveTime =
                        _dateTimeProvider.UtcNow.Subtract(
                            TimeSpan.FromSeconds(_settings.BsReadyTerminatingIntervalSec));
                    var playingInactiveTime =
                        _dateTimeProvider.UtcNow.Subtract(
                            TimeSpan.FromSeconds(_settings.BsPlayingTerminatingIntervalSec));
                    var abandonedServers = await dbContext.BattleServers
                        .Where(bs => bs.State == BattleServerState.Ready && bs.LastUpdate <= readyInactiveTime
                                     || (bs.State == BattleServerState.Playing || bs.State == BattleServerState.Waiting) && bs.LastUpdate <= playingInactiveTime)
                        .ToListAsync(stoppingToken);

                    await Task.WhenAll(abandonedServers.Select(async bs =>
                    {
                        try
                        {
                            await _kubernetes.DeleteNamespacedDeploymentAsync(bs.DeploymentName, K8sNamespaces.Tg,
                                cancellationToken: stoppingToken);
                        }
                        catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode == HttpStatusCode.NotFound)
                        {
                            await dbContext.Entry(bs).Reference(b => b.LoadBalancer).LoadAsync(stoppingToken);

                            await ActivateLbAsync(bs.LoadBalancer!, stoppingToken);
                            dbContext.Remove(bs);
                        }

                        bs.State = BattleServerState.Ended;
                        bs.LastUpdate = _dateTimeProvider.UtcNow;
                    }));

                    var terminationsServers = await dbContext.BattleServers
                        .Include(bs => bs.LoadBalancer)
                        .Where(bs => bs.State == BattleServerState.Ended)
                        .ToListAsync(stoppingToken);

                    var terminatedCount = 0;
                    await Task.WhenAll(terminationsServers.Select(async bs =>
                    {
                        try
                        {
                            await _kubernetes.ReadNamespacedDeploymentWithHttpMessagesAsync(
                                bs.DeploymentName, K8sNamespaces.Tg, cancellationToken: stoppingToken);
                        }
                        catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode ==
                                                                    HttpStatusCode.NotFound)
                        {
                            terminatedCount++;
                            await ActivateLbAsync(bs.LoadBalancer!, stoppingToken);
                            dbContext.Remove(bs);
                        }
                    }));
                    await dbContext.SaveChangesAsync(stoppingToken);

                    if (terminatedCount < terminationsServers.Count)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_settings.BsTerminatingWaitingSec), stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingTimeoutSec), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected exception");
                }
            }
        }

        private async Task ActivateLbAsync(LoadBalancer lb, CancellationToken stoppingToken)
        {
            lb.LastUpdate = _dateTimeProvider.UtcNow;

            if (lb.PublicIp is not null)
            {
                lb.State = LoadBalancerState.Active;
                return;
            }

            try
            {
                var svc = await _kubernetes.ReadNamespacedServiceWithHttpMessagesAsync(
                    lb.SvcName, K8sNamespaces.Tg, cancellationToken: stoppingToken);
                var ip = svc.Body.Status.LoadBalancer.Ingress?.FirstOrDefault()?.Ip;
                if (ip is null)
                {
                    lb.State = LoadBalancerState.Inactive;
                }
                else
                {
                    lb.State = LoadBalancerState.Active;
                    lb.PublicIp = ip;
                }
            }
            catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                lb.State = LoadBalancerState.Inactive;
            }
            
        }
    }
}