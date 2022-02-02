using System;

namespace TG.Manager.Service.Entities
{
    public class BattleServer
    {
        public Guid BattleId { get; set; }

        public BattleServerState State { get; set; }

        public int Port { get; set; }
        
        public string? NodeIp { get; set; }

        public string DeploymentName { get; set; } = default!;
        
        public bool Allocated { get; set; }
        
        public DateTime InitializationTime { get; set; }
        
        public DateTime LastUpdate { get; set; }
        
        public NodePort? NodePort { get; set; }
    }
}