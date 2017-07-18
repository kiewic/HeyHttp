using HeyHttp.Core;
using System;

namespace HeyHttp
{
    internal class ColorLoggerFactory : IHeyLoggerFactory
    {
        public HeyLogger GetSessionLogger()
        {
            return new ColorLogger();
        }
    }
}
