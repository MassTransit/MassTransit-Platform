namespace MassTransit.Platform
{
    using System;


    public interface IStartupBusConfigurator
    {
        void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IRegistrationContext<IServiceProvider> context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator;

        bool TryConfigureQuartz(IBusFactoryConfigurator configurator);
    }
}
