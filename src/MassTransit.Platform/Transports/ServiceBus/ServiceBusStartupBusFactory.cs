namespace MassTransit.Platform.Transports.ServiceBus
{
    using System;
    using Azure.ServiceBus.Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


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

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring Azure Service Bus Message Scheduler (enqueue time)");
                    cfg.UseServiceBusMessageScheduler();
                }

                configurator.ConfigureBus(cfg, provider);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ServiceBusOptions>(configuration.GetSection("ASB"));
        }
    }
}
