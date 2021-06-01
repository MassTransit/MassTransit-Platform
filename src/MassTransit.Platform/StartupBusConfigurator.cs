namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Configuration;
    using PrometheusIntegration;
    using Serilog;


    public class StartupBusConfigurator :
        IStartupBusConfigurator
    {
        readonly PlatformOptions _platformOptions;
        readonly Uri _schedulerEndpointAddress;

        public StartupBusConfigurator(PlatformOptions platformOptions)
        {
            _platformOptions = platformOptions;

            _platformOptions.TryGetSchedulerEndpointAddress(out _schedulerEndpointAddress);
        }

        public bool HasSchedulerEndpoint => _schedulerEndpointAddress != null;

        public void ConfigureBus<TEndpointConfigurator>(IBusFactoryConfigurator<TEndpointConfigurator> configurator, IBusRegistrationContext context)
            where TEndpointConfigurator : IReceiveEndpointConfigurator
        {
            if (!string.IsNullOrWhiteSpace(_platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Metrics: {ServiceName}", _platformOptions.Prometheus);

                configurator.UsePrometheusMetrics(serviceName: _platformOptions.Prometheus);
            }

            List<IPlatformStartup> hostingConfigurators = context.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            foreach (var hostingConfigurator in hostingConfigurators)
                hostingConfigurator.ConfigureBus(configurator, context);

            configurator.ConfigureEndpoints(context);
        }

        public bool TryConfigureQuartz(IBusFactoryConfigurator configurator)
        {
            if (_schedulerEndpointAddress == null)
                return false;

            Log.Information("Configuring Quartz Message Scheduler (endpoint: {QuartzAddress}", _schedulerEndpointAddress);
            configurator.UseMessageScheduler(_schedulerEndpointAddress);
            return true;
        }
    }
}
