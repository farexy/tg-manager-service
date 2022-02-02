namespace TG.Manager.Service.Config.Options
{
    public class BsManagerSettings
    {
        public int UtilizationProcessingTimeoutSec { get; set; }
        public int BsTerminatingWaitingSec { get; set; }
        public int BsReadyTerminatingIntervalSec { get; set; }
        public int BsPlayingTerminatingIntervalSec { get; set; }
        public int PoolProcessingTimeoutSec { get; set; }
        public int ServerPoolCount { get; set; }
    }
}