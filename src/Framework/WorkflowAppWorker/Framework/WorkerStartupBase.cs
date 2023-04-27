using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace WorkflowAppWorker.Framework
{
    public abstract class  WorkerStartupBase
    {
        protected IConfiguration Configuration { get; }

        protected IHealthChecksBuilder HealthChecksBuilder { get; set; }

        // ctor
        protected WorkerStartupBase(IConfiguration configuration) =>
            Configuration = configuration;

        protected IServiceCollection ConfigureDefault(
           IServiceCollection services,
           string cloudRoleName)
        {
            WorkerProgramBase.AddWorkerHostingServices(Configuration, services, cloudRoleName);

            return services;
        }

        protected IApplicationBuilder ConfigureDefault(IApplicationBuilder app, IWebHostEnvironment env, string serviceName)
        {
            return app;
        }       
    }
}
