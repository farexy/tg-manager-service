namespace TG.Manager.Service.Config
{
    public static class ServiceConst
    {
        public const string ServiceName = "manager-service";
        public const string ProjectName = "TG.Manager.Service";

        public const string RoutePrefix = ServiceName + "/v{version:apiVersion}/[controller]";
        public const string BattleServerRoutePrefix = "bs/" + RoutePrefix;
        public const string InternalRoutePrefix = "internal/" + RoutePrefix;
    }
}