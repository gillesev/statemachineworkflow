namespace WorkflowAppWorker.Domain.Models
{
    public class WorfklowEventType
    {
        /// <summary>
        /// Workflow step being started event
        /// </summary>
        public const string StartWorkflowStep = "StartWorkflowStep";
        /// <summary>
        /// Workflow error event
        /// </summary>
        public const string RaiseWorkflowError = "RaiseWorkflowError";
    }
}
