using WorkflowAppWorker.Domain.Models;
using WorkflowAppWorker.Domain.Services;

namespace WorkflowAppWorker.Core.Services
{
    public class WorkflowInboundMessageProcessorBase : IWorkflowInboundMessageProcessor
    {
        public Task<int> HandleErrorAsync(WorkflowStepEvent workflowStepRequest)
        {
            throw new NotImplementedException();
        }

        public Task<int> StartStepAsync(WorkflowStepEvent workflowStepRequest)
        {
            throw new NotImplementedException();
        }
    }
}
