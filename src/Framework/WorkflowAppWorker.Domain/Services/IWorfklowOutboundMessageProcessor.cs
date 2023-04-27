using WorkflowAppWorker.Domain.Models;

namespace WorkflowAppWorker.Domain.Services
{
    /// <summary>
    /// Responsible for handling messages going out
    /// </summary>
    public interface IWorfklowOutboundMessageProcessor
    {
        /// <summary>
        /// Requests a workflow to start.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<int> StartWorkflowAsync(StartWorfklowRequest request);
    }
}
