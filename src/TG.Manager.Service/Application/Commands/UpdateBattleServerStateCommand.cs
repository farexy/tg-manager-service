using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Application.Events;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Application.Commands
{
    public record UpdateBattleServerStateCommand(Guid BattleId, BattleServerState State) : IRequest<OperationResult>;
    
    public class UpdateBattleServerStateCommandHandler : IRequestHandler<UpdateBattleServerStateCommand, OperationResult>
    {   
        private readonly ApplicationDbContext _dbContext;
        private readonly IPublisher _publisher;

        public UpdateBattleServerStateCommandHandler(ApplicationDbContext dbContext, IPublisher publisher)
        {
            _dbContext = dbContext;
            _publisher = publisher;
        }

        public async Task<OperationResult> Handle(UpdateBattleServerStateCommand request, CancellationToken cancellationToken)
        {
            var battleServer = await _dbContext.BattleServers
                .FirstOrDefaultAsync(b => b.BattleId == request.BattleId, cancellationToken);
            battleServer.State = request.State;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _publisher.Publish(new BattleStateChangedEvent(request.State, battleServer), cancellationToken);
            
            return OperationResult.Success();
        }
    }
}