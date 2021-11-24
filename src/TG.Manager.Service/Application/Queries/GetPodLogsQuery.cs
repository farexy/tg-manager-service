using System.IO;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using MediatR;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Config;

namespace TG.Manager.Service.Application.Queries
{
    public record GetPodLogsQuery(string Pod) : IRequest<OperationResult<Stream>>;
    
    public class GetPodLogsQueryHandler : IRequestHandler<GetPodLogsQuery, OperationResult<Stream>>
    {
        private readonly IKubernetes _kubernetes;

        public GetPodLogsQueryHandler(IKubernetes kubernetes)
        {
            _kubernetes = kubernetes;
        }

        public async Task<OperationResult<Stream>> Handle(GetPodLogsQuery request, CancellationToken cancellationToken)
        {
            var stream = await _kubernetes.ReadNamespacedPodLogAsync(request.Pod, K8sNamespaces.Tg,
                cancellationToken: cancellationToken);
            return stream;
        }
    }
}