using System;

namespace HeyHttp.Core.Insights
{
    public interface IHeyRequestTelemetry : IDisposable
    {
        string Name { get; set; }
        string ResponseCode { get; set; }
        Uri Url { get; set; }
    }
}
