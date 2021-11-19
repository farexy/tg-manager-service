using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TG.Core.App.OperationResults;
using TG.Manager.Service.Config;
using TG.Manager.Service.Config.Options;
using TG.Manager.Service.Db;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Services;

namespace TG.Manager.Service.Application.Commands
{
    public record SetupRealtimeServerInstanceCommand(Guid BattleId) : IRequest<OperationResult>;
    
    public class SetupRealtimeServerInstanceCommandHandler : IRequestHandler<SetupRealtimeServerInstanceCommand, OperationResult>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IKubernetes _kubernetes;
        private readonly IRealtimeServerDeploymentConfigProvider _realtimeServerDeploymentConfigProvider;
        private readonly PortsRange _portsRange;

        public SetupRealtimeServerInstanceCommandHandler(ApplicationDbContext dbContext, IKubernetes kubernetes,
            IRealtimeServerDeploymentConfigProvider realtimeServerDeploymentConfigProvider, IOptions<PortsRange> portsRange)
        {
            _kubernetes = kubernetes;
            _realtimeServerDeploymentConfigProvider = realtimeServerDeploymentConfigProvider;
            _dbContext = dbContext;
            _portsRange = portsRange.Value;
        }

        public async Task<OperationResult> Handle(SetupRealtimeServerInstanceCommand request, CancellationToken cancellationToken)
        {
            var port = await _dbContext.BattleServers.MaxAsync(s => s.Port, cancellationToken);
            port = port == default
                ? _portsRange.Min
                : port > _portsRange.Max
                    ? _portsRange.Min
                    : port;
            var yaml = Yaml.LoadAllFromString(await _realtimeServerDeploymentConfigProvider.GetDeploymentYamlAsync(port));
            var deployment = (yaml[0] as V1Deployment)!;
            var service = (yaml[1] as V1Service)!;
            
            var battleServer = new BattleServer
            {
                Port = port,
                BattleId = request.BattleId,
                State = BattleServerState.Initializing,
                DeploymentName = deployment.Metadata.Name,
                SvcName = service.Metadata.Name,
            };

            await _dbContext.BattleServers.AddAsync(battleServer, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            var res1 = await _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment,
                K8sNamespaces.Tg, cancellationToken: cancellationToken);
            var res2 = await _kubernetes.CreateNamespacedServiceWithHttpMessagesAsync(service,
                K8sNamespaces.Tg, cancellationToken: cancellationToken);

            return OperationResult.Success();
        }
    }
}