using System;
using System.Threading.Tasks;
using TG.Manager.Service.ServiceClients;

namespace TG.Manager.Service.Services
{
    public class RealtimeServerDeploymentConfigProvider : IRealtimeServerDeploymentConfigProvider
    {
        private const string PortPlaceholder = "##NODE_PORT##";
        private const string BattleIdPlaceholder = "##BATTLE_ID##";
        private readonly IConfigsClient _configsClient;
        private const string ConfigId = "realtime-server-deployment";
        private static string? _cachedDeployment;
        
        public RealtimeServerDeploymentConfigProvider(IConfigsClient configsClient)
        {
            _configsClient = configsClient;
        }

        public async Task<string> GetDeploymentYamlAsync(int port, Guid battleId)
        {
            var content = _cachedDeployment ??= (await _configsClient.GetConfigContentAsync(ConfigId))
                ?? throw new ApplicationException("Invalid deployment config");
            return content
                .Replace(PortPlaceholder, port.ToString())
                .Replace(BattleIdPlaceholder, battleId.ToString());
        }

        public void ResetCache()
        {
            _cachedDeployment = null;
        }
    }
}