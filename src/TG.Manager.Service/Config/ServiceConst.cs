namespace TG.Manager.Service.Config
{
    public static class ServiceConst
    {
        public const string ServiceName = "manager";
        public const string ProjectName = "TG.Manager.Service";

        public const string RoutePrefix = ServiceName + "/v{version:apiVersion}/[controller]";
        public const string InternalRoutePrefix = "internal/" + ServiceName + "/v{version:apiVersion}/[controller]";
    }
}