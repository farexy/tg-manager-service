using System;

namespace TG.Manager.Service.Entities
{
    public class TestBattleServer
    {
        public Guid BattleId { get; set; }

        public BattleServerState State { get; set; }

        public int Port { get; set; }
        
        public string? Ip { get; set; }
        
        public DateTime InitializationTime { get; set; }
        
        public DateTime LastUpdate { get; set; }
    }
}