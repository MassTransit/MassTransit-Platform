namespace MassTransit.Platform.Transports.ActiveMq
{
    using System;
    using ActiveMqTransport;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class ActiveMqStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IRegistrationContext<IServiceProvider> context, IStartupBusConfigurator configurator)
        {
            var options = context.Container.GetRequiredService<IOptions<ActiveMqOptions>>().Value;

            return Bus.Factory.CreateUsingActiveMq(cfg =>
            {
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
                    cfg.UseActiveMqMessageScheduler();
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
