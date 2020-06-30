namespace MassTransit.Platform
{
    using ExtensionsDependencyInjectionIntegration;


    public interface IStartupBusFactory
    {
        void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IStartupBusConfigurator configurator);
    }
}
