using k8s;
using Microsoft.AspNetCore.Hosting;
using TG.Core.App.Extensions;

namespace TG.Manager.Service.Extensions
{
    public static class HostEnvironmentExtensions
    {
        public static KubernetesClientConfiguration GetK8sConfig(this IWebHostEnvironment environment)
        {
            return environment.IsDebug()
                ? KubernetesClientConfiguration.BuildConfigFromConfigFile()
                : KubernetesClientConfiguration.InClusterConfig();
        }
    }
}