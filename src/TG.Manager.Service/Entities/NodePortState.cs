namespace TG.Manager.Service.Entities
{
    public enum NodePortState
    {
        Inactive = 0,
        Initializing = 1,
        Active = 2,
        Busy = 3,
        Terminating = 4,
        Disposing = 5,
    }
}