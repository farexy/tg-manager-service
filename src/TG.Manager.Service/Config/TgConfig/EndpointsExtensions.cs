using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TG.Core.App.InternalCalls;
using TG.Core.App.Services;

namespace TG.Manager.Service.Config.TgConfig
{
    public static class EndpointsExtensions
    {
        public static IEndpointConventionBuilder MapTgConfigs(this IEndpointRouteBuilder endpoints, string serviceName, Action<string> reloadAction)
        {
            return endpoints.MapPost("internal/" + serviceName +"/configs/{id}", ctx =>
            {
                var configId = ctx.Request.RouteValues["id"];
                var apiKey = endpoints.ServiceProvider.GetRequiredService<IOptions<InternalCallsOptions>>().Value.ApiKey;
                if (configId is null
                    || !ctx.Request.Headers.TryGetValue(ConfigHeaders.Signature, out var sign)
                    || sign != Sha256Helper.GetSha256Hash(configId + apiKey))
                {
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return Task.CompletedTask;
                }

                reloadAction(configId as string ?? string.Empty);

                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            });
        }
    }
}