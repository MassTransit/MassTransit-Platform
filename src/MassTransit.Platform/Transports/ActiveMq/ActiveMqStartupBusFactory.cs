namespace MassTransit.Platform.Transports.ActiveMq
{
    using ActiveMqTransport;
    using ExtensionsDependencyInjectionIntegration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class ActiveMqStartupBusFactory :
        IStartupBusFactory
    {
        public void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IStartupBusConfigurator configurator)
        {
            if (!configurator.HasSchedulerEndpoint)
                busConfigurator.AddDelayedMessageScheduler();

            busConfigurator.UsingActiveMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<ActiveMqOptions>>().Value;

                cfg.Host(options.Host, options.Port, h =>
                {
                    if (!string.IsNullOrWhiteSpace(options.User))
                        h.Username(options.User);
                    if (!string.IsNullOrWhiteSpace(options.Pass))
                        h.Password(options.Pass);

                    if (options.UseSsl)
                        h.UseSsl();
                });

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring ActiveMQ Message Scheduler");
                    cfg.UseDelayedMessageScheduler();
                }

                configurator.ConfigureBus(cfg, context);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ActiveMqOptions>(configuration.GetSection("AMQ"));
        }
    }
}
