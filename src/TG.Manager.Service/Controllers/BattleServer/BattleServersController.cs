using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TG.Core.App.Constants;
using TG.Core.App.InternalCalls;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Application.Commands;
using TG.Manager.Service.Config;
using TG.Manager.Service.Errors;
using TG.Manager.Service.Models.Request;

namespace TG.Manager.Service.Controllers.BattleServer
{
    [BattleServerApi]
    [ApiVersion(ApiVersions.V1)]
    [Route(ServiceConst.BattleServerRoutePrefix)]
    public class BattleServersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BattleServersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult> UpdateState([FromQuery] Guid battleId, [FromBody] BattleServerStateRequest request)
        {
            var result = await _mediator.Send(new UpdateBattleServerStateCommand(battleId, request.State));
            return result.ToActionResult()
                .NotFound(AppErrors.NotFound)
                .NoContent();
        }
    }
}