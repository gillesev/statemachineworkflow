namespace WorkflowAppWorker
{
    public class WorkflowStepRequestMessage
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
        /// Workflow step event type used for routing logic.
        /// </summary>
        public string EventType { get; set; }
        /// <summary>
        /// Workflow step identifier
        /// </summary>
        public int StepId { get; set; }
        /// <summary>
        /// (Optional) Name/Value pair collection data
        /// </summary>
        public ICollection<KeyValuePair<string, string>> Data { get; set; }
    }
}
