namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Microsoft.Extensions.DependencyInjection;


    public class StartupBusConfigurator :
        IStartupBusConfigurator
    {
        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IServiceProvider provider)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            configurator.UseHealthCheck(provider);

            var hostingConfigurators = provider.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            foreach (var hostingConfigurator in hostingConfigurators)
                hostingConfigurator.ConfigureBus(configurator, provider);

            configurator.ConfigureEndpoints(provider);
        }
    }
}
