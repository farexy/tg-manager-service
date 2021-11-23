using System.Collections.Generic;

namespace TG.Manager.Service.Models.Response
{
    public record PodsResponse(IEnumerable<PodResponse> Pods);
    public class PodResponse
    {
        public string Name { get; set; } = default!;
    }
}