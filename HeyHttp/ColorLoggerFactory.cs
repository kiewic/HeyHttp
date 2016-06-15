using HeyHttp.Core;
using System;

namespace HeyHttp
{
    internal class ColorLoggerFactory : IHeyLoggerFactory
    {
        public HeyLogger GetLogger()
        {
            return new ColorLogger();
        }
    }
}