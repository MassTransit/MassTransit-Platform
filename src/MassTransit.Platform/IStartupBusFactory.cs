namespace MassTransit.Platform
{
    using System;


    public interface IStartupBusFactory
    {
        IBusControl CreateBus(IServiceProvider provider, IStartupBusConfigurator configurator);
    }
}
