using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TG.Core.App.Constants;
using TG.Core.App.InternalCalls;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Application.Queries;
using TG.Manager.Service.Config;
using TG.Manager.Service.Errors;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Controllers
{
    [InternalApi]
    [ApiVersion(ApiVersions.V1)]
    [Route(ServiceConst.InternalRoutePrefix)]
    public class BattleServersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BattleServersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{battleId}")]
        public async Task<ActionResult<BattleServerResponse>> Get([FromRoute] Guid battleId)
        {
            var result = await _mediator.Send(new GetBattleServerQuery(battleId));
            return result.ToActionResult()
                .NotFound(AppErrors.NotFound)
                .Ok();
        }
    }
}