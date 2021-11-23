using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.TgConfig;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Extensions
{
    public static class EndpointsExtensions
    {
        public static IEndpointConventionBuilder MapReloadDeploymentConfig(this IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapTgConfigs(ServiceConst.ServiceName, configId =>
            {
                if (configId == TgConfigs.RealtimeServerDeployment)
                {
                    endpoints.ServiceProvider.GetRequiredService<IRealtimeServerDeploymentConfigProvider>().ResetCache();
                }
            });
        }
    }
}