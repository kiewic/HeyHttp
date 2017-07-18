using HeyHttp.Core.Insights;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace HeyHttp
{
    public class HttpInsights : IHeyApplicationInsights
    {
        public class TelemetryOperation : IHeyRequestTelemetry, IDisposable
        {
            IOperationHolder<RequestTelemetry> operationHolder;

            public TelemetryOperation()
            {
                this.operationHolder = telemetry.StartOperation<RequestTelemetry>("HttpClientRequest");
            }

            public string ResponseCode
            {
                get
                {
                    return operationHolder.Telemetry.ResponseCode;
                }
                set
                {
                    operationHolder.Telemetry.ResponseCode = value;
                }
            }

            public Uri Url
            {
                get
                {
                    return operationHolder.Telemetry.Url;
                }
                set
                {
                    operationHolder.Telemetry.Url = value;
                }
            }

            public void Dispose()
            {
                if (this.operationHolder != null)
                {
                    this.operationHolder.Dispose();
                    this.operationHolder = null;
                }
            }

            ~TelemetryOperation()
            {
                Dispose();
            }
        }

        private static TelemetryClient telemetry;

        static HttpInsights()
        {
            TelemetryConfiguration.Active.InstrumentationKey = "0f9f3099-d4e7-45b5-a197-46361d70d209";
            telemetry = new TelemetryClient();
        }

        public IHeyRequestTelemetry StartRequestTelemetry(string ipAddress)
        {
            return new TelemetryOperation();
        }
    }
}
