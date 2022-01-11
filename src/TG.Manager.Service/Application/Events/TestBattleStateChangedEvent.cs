using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TG.Core.App.Services;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Events
{
    public record TestBattleStateChangedEvent(Guid BattleId, BattleServerState State) : INotification;
    
    public class TestBattleStateChangedEventHandler : INotificationHandler<TestBattleStateChangedEvent>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITestBattlesHelper _testBattlesHelper;

        public TestBattleStateChangedEventHandler(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider,
            ITestBattlesHelper testBattlesHelper)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _testBattlesHelper = testBattlesHelper;
        }

        public async Task Handle(TestBattleStateChangedEvent notification, CancellationToken cancellationToken)
        {
            var battleServer = await _dbContext.TestBattleServers
                .FirstOrDefaultAsync(b => b.BattleId == notification.BattleId, cancellationToken);

            if (battleServer is null)
            {
                battleServer = new TestBattleServer
                {
                    BattleId = notification.BattleId,
                };
                await _dbContext.AddAsync(battleServer, cancellationToken);
            }
            battleServer.State = notification.State;
            battleServer.LastUpdate = _dateTimeProvider.UtcNow;

            if (notification.State is BattleServerState.Ready)
            {
                battleServer.InitializationTime = _dateTimeProvider.UtcNow;
                battleServer.Port = _testBattlesHelper.GetPort(notification.BattleId);
                battleServer.Ip = _testBattlesHelper.GetIp(notification.BattleId);
            }
            if (notification.State is BattleServerState.Ended)
            {
                battleServer.State = BattleServerState.Initializing;
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}