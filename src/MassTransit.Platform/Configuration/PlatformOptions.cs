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
        public const string AzureServiceBus = "servicebus";
        public const string AmazonSqs = "sqs";
        public const string ActiveMq = "activemq";

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
        /// If specified, is the queue name of the endpoint where Quartz is running
        /// </summary>
        public string Quartz { get; set; }

        public bool TryGetQuartzEndpointAddress(out Uri address)
        {
            if (!string.IsNullOrWhiteSpace(Quartz))
            {
                if (Uri.IsWellFormedUriString(Quartz, UriKind.Absolute))
                {
                    address = new Uri(Quartz);
                    return true;
                }

                if (Transport.Equals(RabbitMq, StringComparison.OrdinalIgnoreCase) && RabbitMqEntityNameValidator.Validator.IsValidEntityName(Quartz))
                {
                    address = new Uri($"exchange:{Quartz}");
                    return true;
                }

                if ((Transport.Equals(ActiveMq, StringComparison.OrdinalIgnoreCase)
                        && ActiveMqEntityNameValidator.Validator.IsValidEntityName(Quartz))
                    || (Transport.Equals(AzureServiceBus, StringComparison.OrdinalIgnoreCase)
                        && ServiceBusEntityNameValidator.Validator.IsValidEntityName(Quartz))
                    || (Transport.Equals(AmazonSqs, StringComparison.OrdinalIgnoreCase)
                        && AmazonSqsEntityNameValidator.Validator.IsValidEntityName(Quartz)))
                {
                    address = new Uri($"queue:{Quartz}");
                    return true;
                }
            }

            address = default;
            return false;
        }
    }
}
