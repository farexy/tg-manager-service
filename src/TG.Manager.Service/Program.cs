using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using TG.Core.App.Configuration;
using TG.Core.App.Configuration.TgConfig;
using TG.Manager.Service.Config;

namespace TG.Manager.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureTgKeyVault()
                .ConfigureAppConfiguration((ctx, configuration) =>
                {
                    configuration.AddTgConfigs(TgConfigs.BattleSettings);
                })                //.ConfigureTgLogging(ServiceConst.ServiceName)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
