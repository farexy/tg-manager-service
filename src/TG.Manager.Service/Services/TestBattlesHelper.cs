using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TG.Core.App.Extensions;
using TG.Manager.Service.Config.Options;

namespace TG.Manager.Service.Services
{
    public class TestBattlesHelper : ITestBattlesHelper
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly BattleSettings _battleSettings;

        public TestBattlesHelper(IHostEnvironment hostEnvironment, IOptionsSnapshot<BattleSettings> battleSettings)
        {
            _hostEnvironment = hostEnvironment;
            _battleSettings = battleSettings.Value;
        }
        
        public bool IsTestServer(Guid battleId)
        {
            if (_hostEnvironment.IsDevelopmentOrDebug())
            {
                return false;
            }
            return _battleSettings.TestServers.ContainsKey(battleId);
        }

        public string GetIp(Guid testBattleId)
        {
            return _battleSettings.TestServers[testBattleId].Ip;
        }

        public int GetPort(Guid testBattleId)
        {
            return _battleSettings.TestServers[testBattleId].Port;
        }

        public string GetSvcName(Guid testBattleId)
        {
            return _battleSettings.TestServers[testBattleId].Type.ToString();
        }

        public bool IsTestLb(string svcName)
        {
            return nameof(BattleServerType.Static) == svcName || nameof(BattleServerType.Local) == svcName;
        }
    }
}