using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Db;
using TG.Manager.Service.Errors;
using TG.Manager.Service.Models.Response;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Queries
{
    public record GetBattleServerQuery(Guid BattleId) : IRequest<OperationResult<BattleServerResponse>>;
    
    public class GetBattleServerQueryHandler : IRequestHandler<GetBattleServerQuery, OperationResult<BattleServerResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ITestBattlesHelper _testBattlesHelper;

        public GetBattleServerQueryHandler(ApplicationDbContext dbContext, IMapper mapper, ITestBattlesHelper testBattlesHelper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _testBattlesHelper = testBattlesHelper;
        }

        public async Task<OperationResult<BattleServerResponse>> Handle(GetBattleServerQuery request, CancellationToken cancellationToken)
        {
            if (_testBattlesHelper.IsTestServer(request.BattleId))
            {
                return await ReturnTestServerAsync(request.BattleId, cancellationToken);
            }
            var battleServer = await _dbContext.BattleServers
                .Include(bs => bs.NodePort)
                .FirstOrDefaultAsync(bs => bs.BattleId == request.BattleId, cancellationToken);
            if (battleServer is null)
            {
                return AppErrors.NotFound;
            }

            return _mapper.Map<BattleServerResponse>(battleServer);
        }

        private async Task<OperationResult<BattleServerResponse>> ReturnTestServerAsync(Guid battleId, CancellationToken cancellationToken)
        {
            var battleServer = await _dbContext.TestBattleServers
                .FirstOrDefaultAsync(bs => bs.BattleId == battleId, cancellationToken);
            if (battleServer is null)
            {
                return AppErrors.NotFound;
            }

            return _mapper.Map<BattleServerResponse>(battleServer);
        }
    }
}