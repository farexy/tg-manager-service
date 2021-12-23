using System.Threading;
using System.Threading.Tasks;

namespace TG.Manager.Service.Services
{
    public interface INodeProvider
    {
        Task<string?> TryGetNodeIpWithRetryAsync(string appName, CancellationToken cancellationToken);
    }
}