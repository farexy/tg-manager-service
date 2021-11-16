using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Application.Commands
{
    public record UpdateBattleServerStateCommand(int Port, BattleServerState State) : IRequest<OperationResult>;
    
    public class UpdateBattleServerStateCommandHandler : IRequestHandler<UpdateBattleServerStateCommand, OperationResult>
    {
        private readonly ApplicationDbContext _dbContext;

        public UpdateBattleServerStateCommandHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<OperationResult> Handle(UpdateBattleServerStateCommand request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}