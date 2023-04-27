namespace WorkflowAppWorker.Domain.Models
{
    /// <summary>
    /// Workflow Service Bus Configuration.
    /// </summary>
    public class WorkflowServiceBusConfiguration
    {
        public const string WorkflowStepRequestServiceBusConnection = "MessagingConfig:WorkflowStepRequestServiceBusConnection";

        public const string WorkflowStepRequestServiceBusTopicName = "workflow-step-request";
        public const string WorkflowStepRequestServiceBusTopicSubscription = "workflow-step-request-processor";

        public const string WorkflowInstanceStartRequestServiceBusConnection = "MessagingConfig:WorkflowInstanceStartRequestServiceBusConnection";
        public const string WorkflowInstanceStartRequestServiceBusTopicName = "workflow-instance-start-request";
        public const string WorkflowInstanceStartRequestServiceBusClient = "WorkflowInstanceStartRequestServiceBusClient";

        public const string WorkflowStepRequestServiceBusListenClient = "WorkflowStepRequestServiceBusListenClient";

        public const int WorkflowStepRequestMaxAutoLockRenewalDuration = 30;
        public const int WorkflowStepRequestMaxConcurrentCalls = 25;
    }
}
