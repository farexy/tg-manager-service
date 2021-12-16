namespace TG.Manager.Service.Config.Options
{
    public class BsManagerSettings
    {
        public int ProcessingTimeoutSec { get; set; }
        public int BsTerminatingWaitingSec { get; set; }
        public int BsReadyTerminatingIntervalSec { get; set; }
        public int BsPlayingTerminatingIntervalSec { get; set; }
    }
}