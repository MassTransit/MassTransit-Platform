namespace MassTransit.Platform.Configuration
{
    using System;
    using ActiveMqTransport.Topology;
    using AmazonSqsTransport.Topology;
    using Azure.ServiceBus.Core.Topology;
    using RabbitMqTransport.Topology;


    public class PlatformOptions
    {
        public const string RabbitMq = "rabbitmq";
        public const string RMQ = "rmq";
        public const string AzureServiceBus = "servicebus";
        public const string ASB = "asb";
        public const string AmazonSqs = "sqs";
        public const string ActiveMq = "activemq";
        public const string AMQ = "amq";

        public PlatformOptions()
        {
            Transport = RabbitMq;
        }

        public string Transport { get; set; }

        /// <summary>
        /// If specified, Prometheus metrics are enabled for the specified service name
        /// </summary>
        public string Prometheus { get; set; }

        /// <summary>
        /// If specified, is the queue name of the endpoint where the message scheduler is running (if using Quartz or HangFire)
        /// </summary>
        public string Scheduler { get; set; }

        public bool TryGetSchedulerEndpointAddress(out Uri address)
        {
            if (!string.IsNullOrWhiteSpace(Scheduler))
            {
                if (Uri.IsWellFormedUriString(Scheduler, UriKind.Absolute))
                {
                    address = new Uri(Scheduler);
                    return true;
                }

                switch (Transport.ToLowerInvariant())
                {
                    case RabbitMq when RabbitMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case RMQ when RabbitMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                        address = new Uri($"exchange:{Scheduler}");
                        return true;

                    case ActiveMq when ActiveMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case AMQ when ActiveMqEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case AzureServiceBus when ServiceBusEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case ASB when ServiceBusEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                    case AmazonSqs when AmazonSqsEntityNameValidator.Validator.IsValidEntityName(Scheduler):
                        address = new Uri($"queue:{Scheduler}");
                        return true;
                }
            }

            address = default;
            return false;
        }
    }
}
