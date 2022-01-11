using System;

namespace TG.Manager.Service.Entities
{
    public class NodePort
    {
        public int Port { get; set; }

        public string? SvcName { get; set; }

        public NodePortState State { get; set; }
        
        public DateTime LastUpdate { get; set; }
    }
}