namespace HeyHttp.Core
{
    public class HeyUdpSenderSettings : IHeyHostnameSettings, IHeyPortSettings
    {
        public string Hostname { get; set; }

        public int Port { get; set; }

        public string Message { get; set; }
    }
}
