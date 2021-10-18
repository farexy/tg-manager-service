using System.Collections.Generic;

namespace TG.Manager.Service.Models.Response
{
    public record AppEndpointAddressesResponse(IEnumerable<AppEndpointAddressResponse>? Endpoints);

    public class AppEndpointAddressResponse
    {
        public string Ip { get; set; } = default!;
    }
}