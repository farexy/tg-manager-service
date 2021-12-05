using System;

namespace TG.Manager.Service.Entities
{
    public class BattleServer
    {
        public Guid BattleId { get; set; }

        public BattleServerState State { get; set; }

        public int LoadBalancerPort { get; set; }

        public string DeploymentName { get; set; } = default!;
        
        public DateTime InitializationTime { get; set; }
        
        public DateTime LastUpdate { get; set; }
        
        public LoadBalancer? LoadBalancer { get; set; }
    }
}