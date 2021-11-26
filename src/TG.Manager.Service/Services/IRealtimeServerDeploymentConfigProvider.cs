using System;
using System.Threading.Tasks;

namespace TG.Manager.Service.Services
{
    public interface  IRealtimeServerDeploymentConfigProvider
    {
        Task<string> GetDeploymentYamlAsync(int port, Guid battleId);
        void ResetCache();
    }
}