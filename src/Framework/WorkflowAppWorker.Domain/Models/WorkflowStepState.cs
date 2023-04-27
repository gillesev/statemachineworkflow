namespace WorkflowAppWorker.Domain.Models
{
    /// <summary>
    /// Workflow execution step.
    /// Fed to the workflow engine runtime.
    /// </summary>
    public class WorkflowStepState
    {
        /// <summary>
        /// Workflow Step id
        /// </summary>
        public int StepId { get; private set; }

        /// <summary>
        /// Pending = 1, Active = 2, Complete = 3, Canceled = 4, Failed = 5, Skipped = 6
        /// </summary>
        public int Status { get; private set; }

        public WorkflowStepState(int stepId, int status)
        {
            StepId = stepId;
            Status = status;
        }
    }
}
