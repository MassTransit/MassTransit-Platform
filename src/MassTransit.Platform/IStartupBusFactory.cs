namespace MassTransit.Platform
{
    using System;


    public interface IStartupBusFactory
    {
        IBusControl CreateBus(IRegistrationContext<IServiceProvider> context, IStartupBusConfigurator configurator);
    }
}
