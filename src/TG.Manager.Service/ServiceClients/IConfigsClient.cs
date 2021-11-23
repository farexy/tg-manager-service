using System.Threading.Tasks;
using RestEase;

namespace TG.Manager.Service.ServiceClients
{
    public interface IConfigsClient
    {
        [Get("v1/{id}/content")]
        Task<string> GetConfigContentAsync([Path] string id);
    }
}