using System.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility;

namespace WorkflowAppWorker.Framework
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add cloud role name to any kind of telemetry.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <param name="cloudRoleName">The value of cloud role name.</param>
        /// <returns>The collection of services.</returns>
        public static IServiceCollection AddCloudRoleName(
            this IServiceCollection services,
            string cloudRoleName)
        {
            _ = services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>(
                _ => new CloudRoleNameTelemetryInitializer(cloudRoleName));

            Trace.TraceInformation(cloudRoleName + ":: Application Insights telemetry enabled.");

            return services;
        }

        /// <summary>
        /// Always log exception telemetry in App Insights.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <returns>The collection of services.</returns>
        public static IServiceCollection AlwaysLogExceptionTelemetryInApplicationInsights(
            this IServiceCollection services) =>
                services.AddSingleton<ITelemetryInitializer, ExceptionTelemetryInitializer>();
    }

}
