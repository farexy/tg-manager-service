using System.Threading;
using System.Threading.Tasks;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Services;

public interface IServerPreparer
{
    Task<BattleServer> PrepareAsync(bool allocate, CancellationToken cancellationToken);
}