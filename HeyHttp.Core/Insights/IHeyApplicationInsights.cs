namespace HeyHttp.Core.Insights
{
    public interface IHeyApplicationInsights
    {
        IHeyRequestTelemetry StartRequestTelemetry(string ipAddress);
    }
}
