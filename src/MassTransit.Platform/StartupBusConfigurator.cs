namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using PrometheusIntegration;
    using Serilog;


    public class StartupBusConfigurator :
        IStartupBusConfigurator
    {
        readonly PlatformOptions _platformOptions;

        public StartupBusConfigurator(PlatformOptions platformOptions)
        {
            _platformOptions = platformOptions;
        }

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator,
            IRegistrationContext<IServiceProvider> context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            configurator.UseHealthCheck(context);

            if (!string.IsNullOrWhiteSpace(_platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Metrics: {ServiceName}", _platformOptions.Prometheus);

                configurator.UsePrometheusMetrics(serviceName: _platformOptions.Prometheus);
            }

            var hostingConfigurators = context.Container.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            foreach (var hostingConfigurator in hostingConfigurators)
                hostingConfigurator.ConfigureBus(configurator, context);

            configurator.ConfigureEndpoints(context);
        }

        public bool TryConfigureQuartz(IBusFactoryConfigurator configurator)
        {
            if (_platformOptions.TryGetSchedulerEndpointAddress(out var address))
            {
                Log.Information("Configuring Quartz Message Scheduler (endpoint: {QuartzAddress}", address);
                configurator.UseMessageScheduler(address);
                return true;
            }

            return false;
        }
    }
}
