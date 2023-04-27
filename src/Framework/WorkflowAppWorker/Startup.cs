using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.FeatureManagement;
using WorkflowAppWorker.Core.Services;
using WorkflowAppWorker.Domain.Models;
using WorkflowAppWorker.Domain.Services;
using WorkflowAppWorker.Framework;

namespace WorkflowAppWorker
{
    public class Startup : WorkerStartupBase
    {
        private const string CloudRoleName = "WorkflowAppWorker";

        public Startup(IConfiguration configuration) : base(configuration)
        { }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureDefault(services, CloudRoleName);

            // add your own services, including the IHostedService.
            services.AddHostedService<WorkflowAppWorker>();

            // add service bus instances on azure client factory
            var wfStepRequestServiceBusConnectionString = Configuration.GetSection(WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusConnection);
            if (string.IsNullOrWhiteSpace(wfStepRequestServiceBusConnectionString.Value))
            {
                throw new Exception($"Workflow Step Request Service Bus Connection String: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusConnection} not found. Abort.");
            }

            var wfInstanceStartRequestServiceBusConnectionString = Configuration.GetSection(WorkflowServiceBusConfiguration.WorkflowInstanceStartRequestServiceBusConnection);

            services.AddAzureClients(builder =>
            {
                builder.AddServiceBusClient(wfStepRequestServiceBusConnectionString)
                    .WithName(WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusListenClient);

                builder.AddServiceBusClient(wfInstanceStartRequestServiceBusConnectionString)
                    .WithName(WorkflowServiceBusConfiguration.WorkflowInstanceStartRequestServiceBusClient);
            });

            services.AddSingleton<IWorkflowInboundMessageProcessor, WorkflowInboundMessageProcessorBase>();

            services.AddFeatureManagement();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ConfigureDefault(app, env, CloudRoleName);
        }
    }
}
