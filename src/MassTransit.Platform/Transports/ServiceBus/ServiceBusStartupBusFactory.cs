namespace MassTransit.Platform.Transports.ServiceBus
{
    using ExtensionsDependencyInjectionIntegration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class ServiceBusStartupBusFactory :
        IStartupBusFactory
    {
        public void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IStartupBusConfigurator configurator)
        {
            if (!configurator.HasSchedulerEndpoint)
                busConfigurator.AddServiceBusMessageScheduler();

            busConfigurator.UsingAzureServiceBus((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                    throw new ConfigurationException("The Azure Service Bus ConnectionString must not be empty.");

                cfg.Host(options.ConnectionString);

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring Azure Service Bus Message Scheduler (enqueue time)");
                    cfg.UseServiceBusMessageScheduler();
                }

                configurator.ConfigureBus(cfg, context);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ServiceBusOptions>(configuration.GetSection("ASB"));
        }
    }
}
