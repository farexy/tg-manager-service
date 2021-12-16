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
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Commands
{
    public record UpdateBattleServerStateCommand(Guid BattleId, BattleServerState State) : IRequest<OperationResult>;
    
    public class UpdateBattleServerStateCommandHandler : IRequestHandler<UpdateBattleServerStateCommand, OperationResult>
    {   
        private readonly ApplicationDbContext _dbContext;
        private readonly IPublisher _publisher;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITestBattlesHelper _testBattlesHelper;

        public UpdateBattleServerStateCommandHandler(ApplicationDbContext dbContext, IPublisher publisher,
            IDateTimeProvider dateTimeProvider, ITestBattlesHelper testBattlesHelper)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _dateTimeProvider = dateTimeProvider;
            _testBattlesHelper = testBattlesHelper;
        }

        public async Task<OperationResult> Handle(UpdateBattleServerStateCommand request, CancellationToken cancellationToken)
        {
            if (_testBattlesHelper.IsTestServer(request.BattleId))
            {
                await _publisher.Publish(new TestBattleStateChangedEvent(request.BattleId, request.State), cancellationToken);
                return OperationResult.Success();
            }

            var battleServer = await _dbContext.BattleServers
                .Include(bs => bs.LoadBalancer)
                .FirstOrDefaultAsync(b => b.BattleId == request.BattleId, cancellationToken);

            if (battleServer is null)
            {
                return AppErrors.NotFound;
            }

            if (request.State < battleServer.State)
            {
                return AppErrors.InvalidBattleState;
            }
            
            battleServer.State = request.State;
            battleServer.LastUpdate = _dateTimeProvider.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _publisher.Publish(new BattleStateChangedEvent(request.State, battleServer), cancellationToken);
            
            return OperationResult.Success();
        }
    }
}