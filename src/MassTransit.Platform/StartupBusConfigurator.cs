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

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IServiceProvider provider)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            configurator.UseHealthCheck(provider);

            if (!string.IsNullOrWhiteSpace(_platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Metrics: {ServiceName}", _platformOptions.Prometheus);

                configurator.UsePrometheusMetrics(serviceName: _platformOptions.Prometheus);
            }

            var hostingConfigurators = provider.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            foreach (var hostingConfigurator in hostingConfigurators)
                hostingConfigurator.ConfigureBus(configurator, provider);

            configurator.ConfigureEndpoints(provider);
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
