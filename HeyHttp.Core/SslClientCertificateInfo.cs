using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    class SslClientCertificateInfo
    {
        private static Dictionary<int, string> cache = new Dictionary<int, string>();

        internal static void Add(int sslStreamHash, string info)
        {
            cache.Add(sslStreamHash, info);
        }

        internal static bool Remove(int sslStreamHash)
        {
            return cache.Remove(sslStreamHash);
        }

        internal static bool TryGet(int sslStreamHash, out string info)
        {
            info = String.Empty;

            if (!cache.ContainsKey(sslStreamHash))
            {
                return false;
            }

            info = cache[sslStreamHash];
            return true;
        }
    }
}
