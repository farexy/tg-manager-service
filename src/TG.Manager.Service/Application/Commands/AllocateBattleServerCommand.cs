using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Models.Response;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Commands;

public record AllocateBattleServerCommand : IRequest<OperationResult<BattleServerResponse>>;

public class AllocateBattleServerCommandHandler : IRequestHandler<AllocateBattleServerCommand, OperationResult<BattleServerResponse>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IServerPreparer _serverPreparer;
    private readonly IMapper _mapper;

    public AllocateBattleServerCommandHandler(ApplicationDbContext dbContext, IServerPreparer serverPreparer, IMapper mapper)
    {
        _dbContext = dbContext;
        _serverPreparer = serverPreparer;
        _mapper = mapper;
    }

    public async Task<OperationResult<BattleServerResponse>> Handle(AllocateBattleServerCommand cmd, CancellationToken cancellationToken)
    {
        var battleServer = await AllocateWithRetryAsync(0, cancellationToken);
        return _mapper.Map<BattleServerResponse>(battleServer);
    }

    private async Task<BattleServer> AllocateWithRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        const int maxAttempt = 5;
        if (attempt > maxAttempt)
        {
            throw new ApplicationException("Unable to allocate server");
        }
        var battleServer = await _dbContext.BattleServers
            .OrderByDescending(bs => bs.State)
            .FirstOrDefaultAsync(bs => !bs.Allocated, cancellationToken);

        if (battleServer is null)
        {
            battleServer = await _serverPreparer.PrepareAsync(true, cancellationToken);
        }
        else
        {
            battleServer.Allocated = true;
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return await AllocateWithRetryAsync(attempt + 1, cancellationToken);
            }
        }

        return battleServer;
    }
}