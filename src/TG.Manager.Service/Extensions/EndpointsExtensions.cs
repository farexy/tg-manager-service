using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TG.Core.App.Configuration.TgConfig;
using TG.Manager.Service.Config;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Extensions
{
    public static class EndpointsExtensions
    {
        public static IEndpointConventionBuilder MapReloadDeploymentConfig(this IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapTgConfigs(ServiceConst.ServiceName, new Dictionary<string,Action<string>>
            {
                [TgConfigs.RealtimeServerDeployment] = _ => 
                        endpoints.ServiceProvider.GetRequiredService<IRealtimeServerDeploymentConfigProvider>().ResetCache()
            });
        }
    }
}