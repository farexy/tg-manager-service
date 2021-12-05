using System;

namespace TG.Manager.Service.Entities
{
    public class LoadBalancer
    {
        public int Port { get; set; }

        public string SvcName { get; set; } = default!;
        
        public string? PublicIp { get; set; }
        
        public LoadBalancerState State { get; set; }
        
        public DateTime LastUpdate { get; set; }
    }
}