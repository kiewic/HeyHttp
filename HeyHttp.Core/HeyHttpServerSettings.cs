namespace HeyHttp.Core
{
    public class HeyHttpServerSettings : IHeyPortSettings
    {
        public int Port { get; set; }

        public bool OnlyHeadersLog { get; set; }

        public IHeyLoggerFactory LoggerFactory { get; set; }
    }
}
