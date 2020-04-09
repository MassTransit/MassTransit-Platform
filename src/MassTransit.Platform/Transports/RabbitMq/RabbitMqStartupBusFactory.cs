namespace MassTransit.Platform.Transports.RabbitMq
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;


    public class RabbitMqStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IServiceProvider provider, IStartupBusConfigurator configurator)
        {
            var options = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(options.Host, options.Port, options.VHost, h =>
                {
                    h.Username(options.User);
                    h.Password(options.Pass);

                    if (options.Ssl)
                    {
                        h.UseSsl(s =>
                        {
                        });
                    }
                });

                configurator.ConfigureBus(cfg, provider);
            });
        }
    }
}
