namespace WorkflowAppWorker.Domain.Models
{
    /// <summary>
    /// Represents a request to start a workflow
    /// </summary>
    public class StartWorfklowRequest
    {
        /// <summary>
        /// Unique Tenant Id
        /// </summary>
        public Guid TenantGuid { get; set; }

        /// <summary>
        /// Workflow Execution Guid - Identifies a single workflow execution
        /// </summary>
        public Guid WorkflowExecutionGuid { get; set; }

        /// <summary>
        /// Workflow State
        /// </summary>
        public List<WorkflowStepState> State { get; set; }

        /// <summary>
        /// Name/Value pair collection data
        /// </summary>
        public ICollection<KeyValuePair<string, string>> Data { get; set; }
    }
}
