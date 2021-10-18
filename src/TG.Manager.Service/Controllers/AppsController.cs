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
    public class AppsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AppsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpGet("endpoints")]
        public async Task<ActionResult<AppEndpointAddressesResponse>> GetEndpoints([FromQuery] string app)
        {
            var query = new GetAppEndpointsQuery(app);
            var result = await _mediator.Send(query);
            return result.ToActionResult()
                .Ok();
        }
    }
}