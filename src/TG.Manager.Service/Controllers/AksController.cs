using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using TG.Manager.Service.Config;

namespace TG.Manager.Service.Controllers
{
    [ApiVersion("1.0")]
    [Route(ServiceConst.RoutePrefix)]
    public class AksController : ControllerBase
    {
        [HttpGet]
        public async Task Test()
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var client = new Kubernetes(config);

            var endpoints = await client.ListNamespacedEndpointsWithHttpMessagesAsync("tg", labelSelector: "app=game-api");
            var deployment = await Yaml.LoadAllFromFileAsync("D:\\repositories\\somnium\\tg-manager-service\\deployment.yaml");

            
            var res = await client.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment[0] as V1Deployment, "tg");
        }
    }
}