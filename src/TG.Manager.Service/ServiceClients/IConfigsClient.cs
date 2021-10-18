using System.Threading.Tasks;
using RestEase;
using TG.Manager.Service.Models.Dto;

namespace TG.Manager.Service.ServiceClients
{
    public interface IConfigsClient
    {
        [Get("v1/{id}")]
        Task<ConfigDto> GetConfigAsync([Path] string id);
    }
}