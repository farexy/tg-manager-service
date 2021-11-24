using System;
using TG.Manager.Service.Entities;

namespace TG.Manager.Service.Models.Response
{
    public class BattleServerResponse
    {
        public BattleServerState State { get; set; }
        
        public Guid BattleId { get; set; }

        public int LoadBalancerPort { get; set; }

        public string? LoadBalancerIp { get; set; }
    }
}