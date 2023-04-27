using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace WorkflowAppWorker.Framework
{
    public class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string cloudRoleName;

        /// <summary>
        /// Constructor
        /// </summary>
        public CloudRoleNameTelemetryInitializer(string cloudRoleName) => 
            this.cloudRoleName = cloudRoleName ?? 
                throw new ArgumentNullException(nameof(cloudRoleName));

        // Implement ITelemetryInitializer: set cloud role name on any type of telemetry
        public void Initialize(ITelemetry telemetry)
        {
            // validate input
            _ = telemetry ?? throw new ArgumentNullException(nameof(telemetry));

            // set cloud role name
            telemetry.Context.Cloud.RoleName = cloudRoleName;
        }
    }
}
