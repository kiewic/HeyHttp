using System;

namespace HeyHttp.Core
{
    public class HeyTcpClientSettings : IHeyHostnameSettings, IHeyPortSettings
    {
        public string Hostname { get; set; }

        public int Port { get; set; }
    }
}
