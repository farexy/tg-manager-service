using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using TG.Manager.Service.Config;

namespace TG.Manager.Service.Services
{
    public class NodeProvider : INodeProvider
    {
        private readonly ConcurrentDictionary<string, string> NodeIpsCache = new();
        private readonly IKubernetes _kubernetes;

        public NodeProvider(IKubernetes kubernetes)
        {
            _kubernetes = kubernetes;
        }

        public Task<string?> TryGetNodeIpWithRetryAsync(string appName, CancellationToken cancellationToken) =>
            TryGetNodeIpWithRetryAsync(appName, 0, cancellationToken);
        
        private async Task<string?> TryGetNodeIpWithRetryAsync(string appName, int retryCount, CancellationToken cancellationToken)
        {
            const int failRetry = 15;
            const int retryMs = 3000;
            if (retryCount >= failRetry)
            {
                throw new ApplicationException("Can not retrieve load balancer ip. App: " + appName);
            }
            var pods = await _kubernetes.ListNamespacedPodAsync(K8sNamespaces.Tg,
                labelSelector: "app=" + appName, cancellationToken: cancellationToken);

            var nodeName = pods.Items.Single().Spec.NodeName;
            if (NodeIpsCache.TryGetValue(nodeName, out var nodeIp))
            {
                return nodeIp;
            }
            
            var node = await _kubernetes.ReadNodeAsync(nodeName, cancellationToken: cancellationToken);

            var ip = GetNodeIp(node);
            if (ip is null)
            {
                await Task.Delay(retryMs, cancellationToken);
                return await TryGetNodeIpWithRetryAsync(appName, ++retryCount, cancellationToken);
            }

            NodeIpsCache.AddOrUpdate(nodeName, ip, (_,_) => ip);
            return ip;
        }
        
        private static string? GetNodeIp(V1Node node) =>
            node.Status.Addresses.FirstOrDefault(a => a.Type == "ExternalIP")?.Address;
    }
}