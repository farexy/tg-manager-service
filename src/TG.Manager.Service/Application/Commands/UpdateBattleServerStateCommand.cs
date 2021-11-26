using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TG.Core.App.OperationResults;
using TG.Core.App.Services;
using TG.Manager.Service.Application.Events;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Errors;
using TG.Manager.Service.Helpers;

namespace TG.Manager.Service.Application.Commands
{
    public record UpdateBattleServerStateCommand(Guid BattleId, BattleServerState State) : IRequest<OperationResult>;
    
    public class UpdateBattleServerStateCommandHandler : IRequestHandler<UpdateBattleServerStateCommand, OperationResult>
    {   
        private readonly ApplicationDbContext _dbContext;
        private readonly IPublisher _publisher;
        private readonly IDateTimeProvider _dateTimeProvider;

        public UpdateBattleServerStateCommandHandler(ApplicationDbContext dbContext, IPublisher publisher, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<OperationResult> Handle(UpdateBattleServerStateCommand request, CancellationToken cancellationToken)
        {
            if (TestBattles.IsTest(request.BattleId))
            {
                return OperationResult.Success();
            }
            
            var battleServer = await _dbContext.BattleServers
                .FirstOrDefaultAsync(b => b.BattleId == request.BattleId, cancellationToken);

            if (battleServer is null)
            {
                return AppErrors.NotFound;
            }
            battleServer.State = request.State;
            battleServer.LastUpdate = _dateTimeProvider.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _publisher.Publish(new BattleStateChangedEvent(request.State, battleServer), cancellationToken);
            
            return OperationResult.Success();
        }
    }
}