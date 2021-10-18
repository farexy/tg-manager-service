using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using MediatR;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Config;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Commands
{
    public record SetupRealtimeServerInstanceCommand : IRequest<OperationResult>;
    
    public class SetupRealtimeServerInstanceCommandHandler : IRequestHandler<SetupRealtimeServerInstanceCommand, OperationResult>
    {
        private readonly IKubernetes _kubernetes;
        private readonly IRealtimeServerDeploymentConfigProvider _realtimeServerDeploymentConfigProvider;

        public SetupRealtimeServerInstanceCommandHandler(IKubernetes kubernetes, IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider)
        {
            _kubernetes = kubernetes;
            _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
        }

        public async Task<OperationResult> Handle(SetupRealtimeServerInstanceCommand request, CancellationToken cancellationToken)
        {
            
            var deployment = Yaml.LoadAllFromString(await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync());
            var res = await _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment[0] as V1Deployment,
                K8sNamespaces.Tg, cancellationToken: cancellationToken);
            
            return OperationResult.Success();
        }
    }
}