using TG.Core.App.OperationResults;

namespace TG.Manager.Service.Errors
{
    public static class AppErrors
    {
        public static readonly ErrorResult NotFound = new ErrorResult("not_found", "Not found");
    }
}