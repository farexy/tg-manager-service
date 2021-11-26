using System;

namespace TG.Manager.Service.Config
{
    public static class TestBattles
    {
        public static readonly Guid StaticBattleId = Guid.Parse("785d665c-5961-4cde-8024-5f4a1a37d0d9");
        public static readonly Guid LocalBattleId = Guid.Parse("d359772a-854a-4182-a666-1ad49157f9b1");

        public static bool IsTest(Guid battleId) =>
            battleId == StaticBattleId || battleId == LocalBattleId;
    }
}