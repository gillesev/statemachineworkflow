namespace WorkflowAppWorker.Domain.Models
{
    /// <summary>
    /// Represents a request to process a step being started
    /// </summary>
    public class WorkflowStepEvent
    {
        /// <summary>
        /// Unique Tenant Id
        /// </summary>
        public Guid TenantGuid { get; set; }
        /// <summary>
        /// Workflow Execution unique id. Can be used as a correlation id between the app and the workflow engine runtime.
        /// </summary>
        public Guid WorkflowExecutionGuid { get; set; }
        /// <summary>
        /// Workflow Step id. We assume uniqueness for a given workflow id.
        /// This enables efficient logic routing
        /// </summary>
        public int StepId { get; set; }
        /// <summary>
        /// Workflow event type. Enables efficient logic routing.
        /// </summary>
        public string EventType { get; set; }
        /// <summary>
        /// Name/Value pair collection data
        /// </summary>
        public ICollection<KeyValuePair<string, string>> Data { get; set; }
    }
}
