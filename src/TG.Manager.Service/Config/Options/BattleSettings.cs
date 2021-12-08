using System;
using System.Collections.Generic;

namespace TG.Manager.Service.Config.Options
{
    public class BattleSettings
    {
        public Dictionary<string, BattleTypeSettings> BattleTypes { get; set; } = default!;
        public Dictionary<Guid, TestServerSettings> TestServers { get; set; } = default!;
    }
    
    public class BattleTypeSettings
    {
        public int UsersCount { get; set; }
        
        public int ExpectedWaitingTimeSec { get; set; }
        
        public int CostCoins { get; set; }
    }

    public class TestServerSettings
    {
        public BattleServerType Type { get; set; }

        public string Ip { get; set; } = default!;
        
        public int Port { get; set; }
        
        public int ExpectedWaitingTimeSec { get; set; }
    }
}