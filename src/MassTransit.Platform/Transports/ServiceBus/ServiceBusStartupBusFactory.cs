namespace MassTransit.Platform.Transports.ServiceBus
{
    using System;
    using Azure.ServiceBus.Core;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;


    public class ServiceBusStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IServiceProvider provider, IStartupBusConfigurator configurator)
        {
            var options = provider.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new ConfigurationException("The Azure Service Bus ConnectionString must not be empty.");

            return Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            {
                cfg.Host(options.ConnectionString);

                configurator.ConfigureBus(cfg, provider);
            });
        }
    }
}
