namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Abstractions;
    using Configuration;
    using Definition;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using Prometheus;
    using Serilog;
    using Transports.ActiveMq;
    using Transports.AmazonSqs;
    using Transports.RabbitMq;
    using Transports.ServiceBus;


    public class MassTransitStartup
    {
        public MassTransitStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Information("Configuring MassTransit Services");

            services.AddHealthChecks();

            services.Configure<PlatformOptions>(Configuration);
            services.Configure<RabbitMqOptions>(Configuration.GetSection("RabbitMQ"));

            ConfigureApplicationInsights(services);

            var hostingConfigurators = services.BuildServiceProvider().GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
            services.AddMassTransit(cfg =>
            {
                foreach (var hostingConfigurator in hostingConfigurators)
                    hostingConfigurator.ConfigureMassTransit(cfg, services);

                cfg.AddBus(CreateBus);
            });

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Predicate = check => check.Tags.Contains("ready");
            });

            services.AddMassTransitHostedService();
        }

        void ConfigureApplicationInsights(IServiceCollection services)
        {
            if (string.IsNullOrWhiteSpace(Configuration.GetSection("ApplicationInsights")?.GetValue<string>("InstrumentationKey")))
                return;

            Log.Information("Configuring Application Insights");

            services.AddApplicationInsightsTelemetry();

            services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
            {
                module.IncludeDiagnosticSourceActivities.Add("MassTransit");
            });
        }

        IBusControl CreateBus(IServiceProvider provider)
        {
            var platformOptions = provider.GetRequiredService<IOptions<PlatformOptions>>().Value;

            var configurator = new StartupBusConfigurator(platformOptions);

            switch (platformOptions.Transport.ToLower(CultureInfo.InvariantCulture))
            {
                case PlatformOptions.RabbitMq:
                    return new RabbitMqStartupBusFactory().CreateBus(provider, configurator);

                case PlatformOptions.AzureServiceBus:
                    return new ServiceBusStartupBusFactory().CreateBus(provider, configurator);

                case PlatformOptions.ActiveMq:
                    return new ActiveMqStartupBusFactory().CreateBus(provider, configurator);

                case PlatformOptions.AmazonSqs:
                    return new AmazonSqsStartupBusFactory().CreateBus(provider, configurator);

                default:
                    throw new ConfigurationException($"Unknown transport type: {platformOptions.Transport}");
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            var platformOptions = app.ApplicationServices.GetRequiredService<IOptions<PlatformOptions>>().Value;

            app.UseHealthChecks("/health/ready", new HealthCheckOptions {Predicate = check => check.Tags.Contains("ready")});

            app.UseHealthChecks("/health/live");

            if (!string.IsNullOrWhiteSpace(platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Endpoint: /metrics");

                app.UseMetricServer();
            }
        }
    }
}
