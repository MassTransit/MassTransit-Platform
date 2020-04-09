namespace MassTransit.Platform.Transports.ActiveMq
{
    using System;
    using ActiveMqTransport;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;


    public class ActiveMqStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IServiceProvider provider, IStartupBusConfigurator configurator)
        {
            var options = provider.GetRequiredService<IOptions<ActiveMqOptions>>().Value;

            return Bus.Factory.CreateUsingActiveMq(cfg =>
            {
                cfg.Host(options.Host, options.Port, h =>
                {
                    if (!string.IsNullOrWhiteSpace(options.User))
                        h.Username(options.User);
                    if (!string.IsNullOrWhiteSpace(options.Pass))
                        h.Password(options.Pass);

                    if (options.Ssl)
                        h.UseSsl();
                });

                configurator.ConfigureBus(cfg, provider);
            });
        }
    }
}
