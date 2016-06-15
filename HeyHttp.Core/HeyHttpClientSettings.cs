using System.Collections.Generic;

namespace HeyHttp.Core
{
    public class HeyHttpClientSettings
    {
        public HeyHttpClientSettings()
        {
            Headers = new List<string>();
        }


        public string UriString { get; set; }

        public string Method { get; set; }

        public IList<string> Headers { get; set; }
    }
}
