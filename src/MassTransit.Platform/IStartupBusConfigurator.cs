namespace MassTransit.Platform
{
    using System;


    public interface IStartupBusConfigurator
    {
        void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IServiceProvider provider)
            where TEndpointConfigurator : IReceiveEndpointConfigurator;
    }
}
