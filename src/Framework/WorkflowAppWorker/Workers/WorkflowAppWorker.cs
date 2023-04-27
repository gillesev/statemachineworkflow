using System.Diagnostics.CodeAnalysis;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using WorkflowAppWorker.Domain.Models;
using WorkflowAppWorker.Domain.Services;
using STT = System.Threading.Tasks;

namespace WorkflowAppWorker
{
    /// <summary>
    /// Workflow inbound message processor worker
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class WorkflowAppWorker : BackgroundService
    {
        private readonly ILogger<WorkflowAppWorker> _logger;
        private readonly int _maxAutoLockRenewalDuration;
        private readonly int _maxConcurrentCalls;
        private const string WorkflowStepRequestMaxAutoLockRenewalDurationConfigParameterKey = "WorkflowStepRequestMaxAutoLockRenewalDuration";
        private const string WorkflowStepRequestMaxConcurrentCallsConfigParameterKey = "WorkflowStepRequestMaxConcurrentCalls";
        private readonly ServiceBusClient _wfStepRequestServiceBusListenClient;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private ServiceBusProcessor _processor;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="logger">Default Logger</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="serviceBusClientFactory">Service bus client Factory to create message listener/sender clients</param>
        /// <param name="tokenAccessor"></param>
        /// <param name="serviceScopeFactory">Used to create a scoped Activity Instance scoped service</param>
        public WorkflowAppWorker(
            ILogger<WorkflowAppWorker> logger,
            IConfiguration configuration,
            IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _wfStepRequestServiceBusListenClient = serviceBusClientFactory.CreateClient(WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusListenClient);

            if ( !int.TryParse(configuration[WorkflowStepRequestMaxAutoLockRenewalDurationConfigParameterKey], out _maxAutoLockRenewalDuration) )
            {
                _maxAutoLockRenewalDuration = WorkflowServiceBusConfiguration.WorkflowStepRequestMaxAutoLockRenewalDuration;
            }
            if ( !int.TryParse(configuration[WorkflowStepRequestMaxConcurrentCallsConfigParameterKey], out _maxConcurrentCalls) )
            {
                _maxConcurrentCalls = WorkflowServiceBusConfiguration.WorkflowStepRequestMaxConcurrentCalls;
            }
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// Background service method called when its starts.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async STT.Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Workflow app worker is starting.");
            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override async STT.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Workflow app worker is running.");

            try
            {
                // We create one Service Bus Topic Subscription Processor.
                // We are using ReceiveMode = ServiceBusReceiveMode.PeekLock to guarantee message delivery
                // We are handling the long process via ServiceBusProcessorOptions.MaxAutoLockRenewalDuration.
                // Where, It will automatically renew the Lock Token while the message processing is not exceeding the value of MaxAutoLockRenewalDuration.
                // We are handling failure processing automatically by the max delivery count. Meaning if the message got delivered wasn't able to be completed by X times.
                // it will be automatically sent to Dead Letter Queue.
                _processor = _wfStepRequestServiceBusListenClient.CreateProcessor(
                     WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName,
                     WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicSubscription,
                     new ServiceBusProcessorOptions
                     {
                         MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(_maxAutoLockRenewalDuration),
                         AutoCompleteMessages = false,
                         ReceiveMode = ServiceBusReceiveMode.PeekLock,
                         MaxConcurrentCalls = _maxConcurrentCalls
                     });

                // configure the message and error handler for the processor to use
                _processor.ProcessMessageAsync += MessageHandler;
                _processor.ProcessErrorAsync += ErrorHandler;

                // start processing
                await _processor.StartProcessingAsync().ConfigureAwait(false);
            }
            catch ( Exception e )
            {
                _logger.LogError(e, @$"Unknown error occurred while running the workflow app worker 
                    for Topic: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName} - 
                    with Subscription: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicSubscription}");
            }
        }

        private async STT.Task MessageHandler(ProcessMessageEventArgs args)
        {
            var message = args.Message;

            // indicates whether the msg is a poison message
            var isPoisonMessage = true;
            string? messageAsStr = null;
            int retCode = ReturnCode.Fatal;

            try
            {
                messageAsStr = Encoding.UTF8.GetString(message.Body);
                _logger.LogInformation("Received Workflow Step Request message: {ActivityMessageAsStr}.", messageAsStr);

                var msg = JsonConvert.DeserializeObject<WorkflowStepRequestMessage>(messageAsStr);

                _logger.LogInformation(@$"Received Workflow Step Request message -
                    Topic: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName}
                    TenantGuid: {msg.TenantGuid} -
                    WorkflowExecutionGuid : {msg.WorkflowExecutionGuid} -
                    EventType  : {msg.EventType} -
                    Data : {msg.Data}.");

                // if the message is invalid, consider it to be a poison message
                // and we will not retry.
                isPoisonMessage = !IsMessageValid(msg, messageAsStr);

                if ( !isPoisonMessage )
                {
                    _logger.LogInformation(@$"Processing Workflow Step Request message -
                        Topic: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName}
                        TenantGuid: {msg.TenantGuid} -
                        WorkflowExecutionGuid : {msg.WorkflowExecutionGuid} -
                        EventType  : {msg.EventType} -
                        Data : {msg.Data}");

                    // TODO: map msg to a StartWorkflowStepRequest
                    var request = new WorkflowStepEvent();

                    switch ( msg.EventType )
                    {
                        case WorfklowEventType.StartWorkflowStep:
                            using ( var scope = _serviceScopeFactory.CreateScope() )
                            {
                                var processor = scope.ServiceProvider.GetService<IWorkflowInboundMessageProcessor>();
                                retCode = await processor.StartStepAsync(request).ConfigureAwait(false);
                            }
                            break;
                        case WorfklowEventType.RaiseWorkflowError:
                            using ( var scope = _serviceScopeFactory.CreateScope() )
                            {
                                var processor = scope.ServiceProvider.GetService<IWorkflowInboundMessageProcessor>();
                                retCode = await processor.HandleErrorAsync(request);
                            }
                            break;
                        default:
                            isPoisonMessage = true;
                            throw new NotSupportedException($"{msg.EventType} workflow event type is not supported.");
                    }

                    _logger.LogInformation(@$"Workflow Step Request message Processed -
                        Return Code: {retCode} - 
                        Topic: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName} - 
                        TenantGuid: {msg.TenantGuid} -
                        WorkflowExecutionGuid : {msg.WorkflowExecutionGuid} -
                        EventType  : {msg.EventType} -
                        Data : {msg.Data}");

                    if (retCode == ReturnCode.Success)
                    {
                        await args.CompleteMessageAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        switch ( retCode )
                        {
                            case ReturnCode.Fatal:
                                await args.DeadLetterMessageAsync(message).ConfigureAwait(false);
                                break;
                            default:
                                await args.AbandonMessageAsync(message).ConfigureAwait(false);
                                break;
                        }
                    }
                }
                else
                {
                    // This is a poison message and is not valid.
                    if ( string.IsNullOrWhiteSpace(messageAsStr) )
                    {
                        _logger.LogError("POISON message - invalid Activity Service Bus Message: {ActivityMessageAsStr}. Will move the message to DLQ.", messageAsStr);
                    }
                    else
                    {
                        _logger.LogError("POISON message - invalid Activity Service Bus Message. Will move the message to DLQ.");
                    }
                    await args.DeadLetterMessageAsync(message).ConfigureAwait(false);
                }
            }
            catch ( Exception e )
            {
                if ( isPoisonMessage )
                {
                    if ( string.IsNullOrWhiteSpace(messageAsStr) )
                    {
                        _logger.LogError(e, "POISON message - invalid Activity Service Bus Message: {ActivityMessageAsStr}. Will move the message to DLQ.", messageAsStr);
                    }
                    else
                    {
                        _logger.LogError(e, "POISON message - invalid Activity Service Bus Message. Will move the message to DLQ.");
                    }

                    // POISON message, put message to dead letter queue immediately.
                    await args.DeadLetterMessageAsync(message).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogError(e, @$"An error occured while processing a workflow request inbound message for -
                        Topic: {WorkflowServiceBusConfiguration.WorkflowStepRequestServiceBusTopicName} - 
                        MessageBody: {messageAsStr}.");


                    switch (retCode)
                    {
                        case ReturnCode.Fatal:
                            await args.DeadLetterMessageAsync(message).ConfigureAwait(false);
                            break;
                        default:
                            await args.AbandonMessageAsync(message).ConfigureAwait(false);
                            break;
                    }

                    // Abandon failed processed message to automatically send back to service bus and increment delivery count.
                    await args.AbandonMessageAsync(message).ConfigureAwait(false);
                }
            }
        }

        private bool IsMessageValid(WorkflowStepRequestMessage msg, string messageAsStr)
        {
            if ( msg.TenantGuid == Guid.Empty )
            {
                _logger.LogError("Invalid Tenant Guid");
                return false;
            }

            if ( msg.WorkflowExecutionGuid == Guid.Empty )
            {
                _logger.LogError("Invalid Workflow Execution Guid.");
                return false;
            }

            if ( msg.EventType != WorfklowEventType.RaiseWorkflowError && msg.StepId <= 0 )
            {
                _logger.LogError("Invalid Workflow Step Request message. Invalid Step Id. {ActivityMessageAsStr}", messageAsStr);
                return false;
            }

            return true;
        }

        /// <summary>
        /// This is the error handler for the service bus processor.
        /// Any kind of error/exception happened in the message handler will be handled in this method.
        /// </summary>
        /// <returns></returns>
        private STT.Task ErrorHandler(ProcessErrorEventArgs args)
        {

            // the error source tells me at what point in the processing an error occurred
            _logger.LogError(@$"Error Source: {args.ErrorSource}");
            // the fully qualified namespace is available
            _logger.LogError(@$"Fully Qualified Namespace: {args.FullyQualifiedNamespace}");
            // as well as the entity path
            _logger.LogError(@$"Entity Path: {args.EntityPath}");
            // log the actual exception
            _logger.LogError(@$"Exception: {args.Exception}");
            return STT.Task.CompletedTask;
        }

        /// <summary>
        /// Stops the Workflow app worker
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async STT.Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Workflow app worker is stopping.");

            if ( _processor != null )
            {
                await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);
            }

            if ( _wfStepRequestServiceBusListenClient != null )
            {
                await _wfStepRequestServiceBusListenClient.DisposeAsync().ConfigureAwait(false);
            }
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
