using System.Threading.Tasks;
using k8s;
using k8s.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Application.Commands;
using TG.Manager.Service.Application.Queries;
using TG.Manager.Service.Config;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Controllers
{
    /// <summary>
    /// Controller is for test purposes and will be removed.  
    /// </summary>
    [ApiVersion("1.0")]
    [Route(ServiceConst.RoutePrefix)]
    public class AksController : ControllerBase
    {
        private readonly IMediator _mediator; 

        public AksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult> SetupServer()
        {
            var command = new SetupRealtimeServerInstanceCommand();
            var result = await _mediator.Send(command);
            return result.ToActionResult()
                .NoContent();
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