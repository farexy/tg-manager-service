using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TG.Core.App.Constants;
using TG.Core.App.InternalCalls;
using TG.Manager.Service.Config;
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

        [HttpPatch("{port}")]
        public async Task<ActionResult> UpdateState([FromRoute] int port, [FromBody] BattleServerStateRequest request)
        {
            var result = await _mediator.Send(new)
        }
    }
}