using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Db;
using TG.Manager.Service.Errors;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Application.Queries
{
    public record GetBattleServerQuery(Guid BattleId) : IRequest<OperationResult<BattleServerResponse>>;
    
    public class GetBattleServerQueryHandler : IRequestHandler<GetBattleServerQuery, OperationResult<BattleServerResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public GetBattleServerQueryHandler(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<OperationResult<BattleServerResponse>> Handle(GetBattleServerQuery request, CancellationToken cancellationToken)
        {
            var battleServer = await _dbContext.BattleServers.FindAsync(request.BattleId);
            if (battleServer is null)
            {
                return AppErrors.NotFound;
            }

            return _mapper.Map<BattleServerResponse>(battleServer);
        }
    }
}