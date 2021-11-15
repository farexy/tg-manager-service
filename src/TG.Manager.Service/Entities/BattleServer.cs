using System;

namespace TG.Manager.Service.Entities
{
    public class BattleServer
    {
        public int Port { get; set; }
        
        public BattleServerState State { get; set; }
        
        public Guid? CurrentBattleId { get; set; }
    }
}