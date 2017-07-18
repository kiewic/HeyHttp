using System;

namespace HeyHttp.Core.Insights
{
    public interface IHeyRequestTelemetry : IDisposable
    {
        string ResponseCode { get; set; }
        Uri Url { get; set; }
    }
}
