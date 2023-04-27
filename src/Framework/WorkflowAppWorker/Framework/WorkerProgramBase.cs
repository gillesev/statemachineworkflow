using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace WorkflowAppWorker.Framework
{
    public class WorkerProgramBase
    {
        private const string LOG_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        /// <summary>
        /// Get Application Insights instrumentation key from configuration.
        /// </summary>
        private static string GetInstrumentationKey(IConfiguration config) =>
                config.GetSection("APPINSIGHTS_INSTRUMENTATIONKEY")?.Value
                    ?? config.GetSection("ApplicationInsights")
                        .GetValue<string>("InstrumentationKey");

        public static Action<IConfiguration, IServiceCollection, string> AddWorkerHostingServices = (configuration, services, cloudRoleName) =>
        {
            services
                .AddOptions()
                .AddSingleton<ITelemetryInitializer, ExceptionTelemetryInitializer>()
                .AddCloudRoleName(cloudRoleName)
                .AddApplicationInsightsTelemetryWorkerService(new Microsoft.ApplicationInsights.WorkerService.ApplicationInsightsServiceOptions
                {
                    InstrumentationKey = GetInstrumentationKey(configuration),
                    EnableAdaptiveSampling = false
                })
                .AddHttpClient()
                .AddAzureAppConfiguration();
        };

        /// <summary>
        /// Call this method from your top level program entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>

        public static async Task RunServerAsync<TType>(
            string[] args,
            IEnumerable<string> appConfigurationFilterKeys,
            IEnumerable<string> appConfigurationRefreshKeys
            )
        {
            IHost host = Host.CreateDefaultBuilder(args)
                 // Configures the Serilog options such as minimum levels and sinks. Sets the static Log.Logger
                 // to the fully configured logger, including the Application Insights sink
                 .UseSerilog((hostBuilderContext, services, loggerConfiguration) =>
                 {
                     var telemetryConfiguration = services.GetRequiredService<TelemetryConfiguration>();
                     loggerConfiguration
                        .MinimumLevel.Debug()
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: LOG_TEMPLATE)
                        .WriteTo.ApplicationInsights(
                             telemetryConfiguration,
                             TelemetryConverter.Traces);
                 })
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseContentRoot(Directory.GetCurrentDirectory())
                         .ConfigureAppConfiguration((_, config) =>
                             ProgramConfiguration.ConfigureAppConfiguration(
                                 config,
                                 appConfigurationFilterKeys,
                                 appConfigurationRefreshKeys))
                         .UseStartup(typeof(TType));
                 })
                .Build();

            await host.RunAsync();
        }
    }
}
