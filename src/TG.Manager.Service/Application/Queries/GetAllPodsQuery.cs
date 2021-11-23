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
    public record GetAllPodsQuery : IRequest<OperationResult<PodsResponse>>;
    
    public class GetAllPodsQueryHandler : IRequestHandler<GetAllPodsQuery, OperationResult<PodsResponse>>
    {
        private readonly IKubernetes _kubernetes;

        public GetAllPodsQueryHandler(IKubernetes kubernetes)
        {
            _kubernetes = kubernetes;
        }

        public async Task<OperationResult<PodsResponse>> Handle(GetAllPodsQuery request, CancellationToken cancellationToken)
        {
            var pods = await _kubernetes.ListNamespacedPodWithHttpMessagesAsync(
                K8sNamespaces.Tg, cancellationToken: cancellationToken);
            return new PodsResponse(pods.Body.Items.Select(pod => new PodResponse
            {
                Name = pod.Metadata.Name,
            }));
        }
    }
}