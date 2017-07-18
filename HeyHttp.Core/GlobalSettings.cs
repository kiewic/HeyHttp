using HeyHttp.Core.Insights;

namespace HeyHttp.Core
{
    public class GlobalSettings
    {
        public static IHeyLoggerFactory LoggerFactory { get; set; }
        public static IHeyApplicationInsights HttpInsights { get; set; }
    }
}
