using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using WorkflowAppWorker.Domain.Models;
using WorkflowAppWorker.Domain.Services;

namespace WorkflowAppWorker.Core.Services
{
    public class WorkflowOutboundMessageProcessorBase : IWorfklowOutboundMessageProcessor
    {
        private readonly ILogger<WorkflowOutboundMessageProcessorBase> _logger;
        private readonly ServiceBusClient _wfInstanceStartRequestServiceBusClient;
        private readonly ServiceBusSender _wfInstanceStartRequestServiceBusTopicName;

        public WorkflowOutboundMessageProcessorBase(
            ILogger<WorkflowOutboundMessageProcessorBase> logger,
            IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wfInstanceStartRequestServiceBusClient = serviceBusClientFactory.CreateClient(WorkflowServiceBusConfiguration.WorkflowInstanceStartRequestServiceBusClient);
            _wfInstanceStartRequestServiceBusTopicName = _wfInstanceStartRequestServiceBusClient.CreateSender(WorkflowServiceBusConfiguration.WorkflowInstanceStartRequestServiceBusTopicName);
        }

        public async Task<int> StartWorkflowAsync(StartWorfklowRequest request)
        {
            try
            {
                _logger.LogInformation("Sending Start Workflow Instance request message.");

                var msg = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)))
                {
                    ContentType = "application/json"
                };

                await _wfInstanceStartRequestServiceBusTopicName.SendMessageAsync(msg).ConfigureAwait(false);

                _logger.LogInformation("Start Workflow Instance request message sent");

                return ReturnCode.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while sending a Start Workflow Instance request message.");
                throw;
            }
        }
    }
}
