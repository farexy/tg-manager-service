using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using MediatR;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Config;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Application.Queries
{
    public record GetAppEndpointsQuery(string App) : IRequest<OperationResult<AppEndpointAddressesResponse>>;

    public class GetAppEndpointsQueryHandler : IRequestHandler<GetAppEndpointsQuery, OperationResult<AppEndpointAddressesResponse>>
    {
        private readonly IKubernetes _kubernetes;

        public GetAppEndpointsQueryHandler(IKubernetes kubernetes)
        {
            _kubernetes = kubernetes;
        }

        public async Task<OperationResult<AppEndpointAddressesResponse>> Handle(GetAppEndpointsQuery request, CancellationToken cancellationToken)
        {
            var endpoints = await _kubernetes.ListNamespacedEndpointsWithHttpMessagesAsync(
                K8sNamespaces.Tg, labelSelector: "app=" + request.App, cancellationToken: cancellationToken);
            return new AppEndpointAddressesResponse(endpoints.Body.Items.FirstOrDefault()?.Subsets.FirstOrDefault()?.Addresses.Select(address => new AppEndpointAddressResponse
            {
                Ip = address.Ip,
            }));
        }
    }
}