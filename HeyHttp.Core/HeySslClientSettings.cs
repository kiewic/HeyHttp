namespace HeyHttp.Core
{
    public class HeySslClientSettings : IHeyHostnameSettings, IHeyPortSettings
    {
        public string Hostname { get; set; }

        public int Port { get; set; }
    }
}
