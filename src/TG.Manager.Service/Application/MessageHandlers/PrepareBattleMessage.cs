using System;

namespace TG.Manager.Service.Application.MessageHandlers
{
    public class PrepareBattleMessage
    {
        public Guid BattleId { get; set; }

        public string BattleType { get; set; } = default!;
    }
}