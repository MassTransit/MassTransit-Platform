namespace MassTransit.Platform.Configuration
{
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
    }
}
