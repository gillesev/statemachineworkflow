using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace WorkflowAppWorker.Framework
{
    public class ExceptionTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Initializes properties of the specified Microsoft.ApplicationInsights.Channel.ITelemetry
        /// object to ensure that exception telemetry is always logged.
        /// </summary>
        /// <param name="telemetry">The telemetry object for Application Insights.</param>
        /// <remarks>
        /// See "There are certain rare events I always want to see. How can I get them past the sampling module?" in
        /// https://docs.microsoft.com/en-us/azure/azure-monitor/app/sampling#frequently-asked-questions
        /// </remarks>
        public void Initialize(ITelemetry telemetry)
        {
            _ = telemetry ?? throw new ArgumentNullException(nameof(telemetry));

            if ( telemetry is ExceptionTelemetry )
            {
                // 100 = sampling will include all telemetry
                ((ISupportSampling)telemetry).SamplingPercentage = 100;
            }
        }
    }
}
