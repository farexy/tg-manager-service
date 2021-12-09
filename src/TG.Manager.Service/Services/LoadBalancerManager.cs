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
    public class LoadBalancerManager : BackgroundService
    {
        private readonly LbManagerSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IKubernetes _kubernetes;
        private readonly ILogger<LoadBalancerManager> _logger;

        public LoadBalancerManager(IServiceProvider serviceProvider, IOptions<LbManagerSettings> settings, IDateTimeProvider dateTimeProvider,
            IKubernetes kubernetes, ILogger<LoadBalancerManager> logger)
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
                    var terminatingTime =
                        _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromSeconds(_settings.LbTerminatingIntervalSec));
                    var inactiveLbs = await dbContext.LoadBalancers
                        .Where(lb => lb.State == LoadBalancerState.Active && lb.LastUpdate <= terminatingTime)
                        .ToListAsync(stoppingToken);

                    inactiveLbs.ForEach(lb =>
                    {
                        lb.State = LoadBalancerState.Terminating;
                        lb.LastUpdate = _dateTimeProvider.UtcNow;
                    });
                    await dbContext.SaveChangesAsync(stoppingToken);
                    
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ProcessingTimeoutSec), stoppingToken);

                    var disposingLbs = await dbContext.LoadBalancers
                        .Where(lb => lb.State == LoadBalancerState.Disposing)
                        .ToListAsync(stoppingToken);
                    await Task.WhenAll(
                        disposingLbs.Select(async lb =>
                        {
                            try
                            {
                                await _kubernetes.ReadNamespacedServiceWithHttpMessagesAsync(lb.SvcName,
                                    K8sNamespaces.Tg, cancellationToken: stoppingToken);
                            }
                            catch (HttpOperationException httpEx) when (httpEx.Response?.StatusCode == HttpStatusCode.NotFound)
                            {
                                lb.State = LoadBalancerState.Inactive;
                                lb.PublicIp = null;
                                lb.LastUpdate = _dateTimeProvider.UtcNow;
                            }
                        })
                    );

                    await dbContext.SaveChangesAsync(stoppingToken);

                    var terminatingLbs = await dbContext.LoadBalancers
                        .Where(lb => lb.State == LoadBalancerState.Terminating)
                        .ToListAsync(stoppingToken);
                    await Task.WhenAll(
                        terminatingLbs.Select(lb =>
                        {
                            lb.State = LoadBalancerState.Disposing;
                            lb.LastUpdate = _dateTimeProvider.UtcNow;
                            return _kubernetes.DeleteNamespacedServiceAsync(lb.SvcName, K8sNamespaces.Tg, cancellationToken: stoppingToken);
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
    }
}