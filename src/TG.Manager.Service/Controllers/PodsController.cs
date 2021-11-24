using System.IO;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TG.Core.App.Constants;
using TG.Core.App.InternalCalls;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Application.Queries;
using TG.Manager.Service.Config;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Controllers
{
    [InternalApi]
    [ApiVersion(ApiVersions.V1)]
    [Route(ServiceConst.InternalRoutePrefix)]
    public class PodsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PodsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<PodsResponse>> GetAllPods()
        {
            var query = new GetAllPodsQuery();
            var result = await _mediator.Send(query);
            return result.ToActionResult()
                .Ok();
        }
        
        [HttpGet("{name}/logs")]
        public async Task<ActionResult<Stream>> GetPodLogs([FromRoute] string name)
        {
            var query = new GetPodLogsQuery(name);
            var result = await _mediator.Send(query);
            return result.ToActionResult()
                .File();
        }
    }
}