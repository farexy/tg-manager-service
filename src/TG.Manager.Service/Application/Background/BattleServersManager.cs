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
    // todo concurrency
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

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.WhenAll(
            ProcessServersUtilizing(stoppingToken),
            ProcessServersPooling(stoppingToken));

        private async Task ProcessServersPooling(CancellationToken stoppingToken)
        {
            // todo pooling
            return;
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    var metricsQuery = from m in dbContext.BattleServers
                        select new
                        {
                            Allocated = dbContext.BattleServers.Count(x => x.Allocated),
                            Free = dbContext.BattleServers.Count(x => !x.Allocated)
                        };
                    var metrics = await metricsQuery.SingleOrDefaultAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(_settings.PoolProcessingTimeoutSec), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected exception in {nameof(ProcessServersPooling)}");
                    await Task.Delay(TimeSpan.FromSeconds(_settings.PoolProcessingTimeoutSec), stoppingToken);
                }
            }
        }


        private async Task ProcessServersUtilizing(CancellationToken stoppingToken)
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
                            await dbContext.Entry(bs).Reference(b => b.NodePort).LoadAsync(stoppingToken);
                            bs.NodePort!.LastUpdate = _dateTimeProvider.UtcNow;
                            bs.NodePort!.State = NodePortState.Active;

                            dbContext.Remove(bs);
                        }

                        bs.State = BattleServerState.Ended;
                        bs.LastUpdate = _dateTimeProvider.UtcNow;
                    }));

                    var terminationsServers = await dbContext.BattleServers
                        .Include(bs => bs.NodePort)
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
                            bs.NodePort!.LastUpdate = _dateTimeProvider.UtcNow;
                            bs.NodePort!.State = NodePortState.Active;
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
                        await Task.Delay(TimeSpan.FromSeconds(_settings.UtilizationProcessingTimeoutSec), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected exception in {nameof(ProcessServersUtilizing)}");
                    await Task.Delay(TimeSpan.FromSeconds(_settings.UtilizationProcessingTimeoutSec), stoppingToken);
                }
            }
        }
    }
}