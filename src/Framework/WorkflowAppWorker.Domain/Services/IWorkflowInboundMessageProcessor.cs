using WorkflowAppWorker.Domain.Models;

namespace WorkflowAppWorker.Domain.Services
{
    /// <summary>
    /// Responsible for handling incoming messages
    /// </summary>
    public interface IWorkflowInboundMessageProcessor
    {
        /// <summary>
        /// Processes a request a workflow step being started
        /// </summary>
        /// <param name="workflowStepRequest"></param>
        /// <returns></returns>
        public Task<int> StartStepAsync(WorkflowStepEvent workflowStepRequest);

        /// <summary>
        /// Handles a workflow error
        /// </summary>
        /// <param name="workflowStepRequest"></param>
        /// <returns></returns>
        public Task<int> HandleErrorAsync(WorkflowStepEvent workflowStepRequest);
    }
}
