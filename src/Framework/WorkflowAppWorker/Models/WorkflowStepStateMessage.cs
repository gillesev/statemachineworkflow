namespace WorkflowAppWorker
{
    /// <summary>
    /// Workflow execution state
    /// </summary>
    public class WorkflowStepStateMessage
    {
        /// <summary>
        /// Workflow Step unique identifier
        /// </summary>
        public int StepId { get; private set; }

        /// <summary>
        /// Pending = 1, Active = 2, Complete = 3, Canceled = 4, Failed = 5, Skipped = 6
        /// </summary>
        public int Status { get; private set; }

        public WorkflowStepStateMessage(int stepId, int status)
        {
            StepId = stepId;
            Status = status;
        }
    }
}
