using System.Threading.Tasks;
using TG.Manager.Service.ServiceClients;

namespace TG.Manager.Service.Services
{
    public class RealtimeServerDeploymentConfigProvider : IRealtimeServerDeploymentConfigProvider
    {
        private readonly IConfigsClient _configsClient;
        private const string ConfigId = "realtime-server-deployment";
        private static string? _cachedDeployment;
        
        public RealtimeServerDeploymentConfigProvider(IConfigsClient configsClient)
        {
            _configsClient = configsClient;
        }

        public async Task<string?> GetDeploymentYamlAsync()
        {
            return _cachedDeployment ??= (await _configsClient.GetConfigAsync(ConfigId))?.Content;
        }

        public void ResetCache()
        {
            _cachedDeployment = null;
        }
    }
}