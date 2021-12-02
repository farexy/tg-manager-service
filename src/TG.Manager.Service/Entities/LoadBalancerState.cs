namespace TG.Manager.Service.Entities
{
    public enum LoadBalancerState
    {
        Inactive = 0,
        Initializing = 1,
        Active = 2,
        Busy = 3,
        Terminating = 3,
    }
}