
namespace WorkflowAppWorker.Models
{
    public class StartWorfklowRequestMessage
    {
        /// <summary>
        /// Unique Tenant Id
        /// </summary>
        public Guid TenantGuid { get; set; }

        /// <summary>
        /// Workflow Execution Guid - Identifies a single workflow execution
        /// Is used when the Logic App Configuration runs in "Replay" mode.
        /// </summary>
        public Guid WorkflowExecutionGuid { get; set; }

        /// <summary>
        /// Workflow State
        /// </summary>
        public List<WorkflowStepStateMessage> State { get; set; }

        /// <summary>
        /// (Optional) Name/Value pair collection data
        /// </summary>
        public ICollection<KeyValuePair<string, string>> Data { get; set; }
    }
}
