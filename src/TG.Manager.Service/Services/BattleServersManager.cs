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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var inactiveTime = _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromSeconds(_settings.BsTerminatingIntervalSec));
                    var abandonedServers = await dbContext.BattleServers
                        .Where(bs => bs.State == BattleServerState.Ready && bs.LastUpdate <= inactiveTime)
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
                            
                            bs.LoadBalancer!.State = LoadBalancerState.Active;
                            bs.LoadBalancer!.LastUpdate = _dateTimeProvider.UtcNow;
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
                        catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode == HttpStatusCode.NotFound)
                        {
                            terminatedCount++;
                            bs.LoadBalancer!.State = LoadBalancerState.Active;
                            bs.LoadBalancer!.LastUpdate = _dateTimeProvider.UtcNow;
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
    }
}