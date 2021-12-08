using System;

namespace TG.Manager.Service.Services
{
    public interface ITestBattlesHelper
    {
        bool IsTestServer(Guid battleId);
        string GetIp(Guid testBattleId);
        int GetPort(Guid testBattleId);
        string GetSvcName(Guid testBattleId);
        bool IsTestLb(string svcName);
    }
}