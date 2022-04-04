namespace MassTransit.Platform
{
    public interface IStartupBusFactory
    {
        void CreateBus(IBusRegistrationConfigurator busConfigurator, IStartupBusConfigurator configurator);
    }
}
