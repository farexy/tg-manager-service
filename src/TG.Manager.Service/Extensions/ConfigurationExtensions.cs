using System;
using Microsoft.Extensions.Configuration;

namespace TG.Manager.Service.Extensions
{
    public static class ConfigurationExtensions
    {
        public static Uri GetConfigsUrl(this IConfiguration configuration) =>
            configuration.GetValue<Uri>("TgConfigs:ServiceUrl");
    }
}