namespace TG.Manager.Service.Entities
{
    public enum NodePortState
    {
        Inactive = 0,
        Initializing = 1,
        Active = 2,
        Busy = 3,
        Reserved = 4,
        Terminating = 5,
        Disposing = 6,
    }
}