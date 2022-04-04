﻿namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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

            RabbitMqStartupBusFactory.Configure(services, Configuration);
            ServiceBusStartupBusFactory.Configure(services, Configuration);
            ActiveMqStartupBusFactory.Configure(services, Configuration);
            AmazonSqsStartupBusFactory.Configure(services, Configuration);

            var configurationServiceProvider = services.BuildServiceProvider();

            List<IPlatformStartup> platformStartups = configurationServiceProvider.GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            ConfigureApplicationInsights(services);

            services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
            services.AddMassTransit(cfg =>
            {
                foreach (var platformStartup in platformStartups)
                    platformStartup.ConfigureMassTransit(cfg, services);

                CreateBus(cfg, configurationServiceProvider);
            });

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
            });
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

        void CreateBus(IBusRegistrationConfigurator busConfigurator, IServiceProvider provider)
        {
            var platformOptions = provider.GetRequiredService<IOptions<PlatformOptions>>().Value;

            var configurator = new StartupBusConfigurator(platformOptions);

            switch (platformOptions.Transport.ToLower(CultureInfo.InvariantCulture))
            {
                case PlatformOptions.RabbitMq:
                case PlatformOptions.RMQ:
                    new RabbitMqStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;

                case PlatformOptions.AzureServiceBus:
                case PlatformOptions.ASB:
                    new ServiceBusStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;

                case PlatformOptions.ActiveMq:
                case PlatformOptions.AMQ:
                    new ActiveMqStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;

                case PlatformOptions.AmazonSqs:
                    new AmazonSqsStartupBusFactory().CreateBus(busConfigurator, configurator);
                    break;

                default:
                    throw new ConfigurationException($"Unknown transport type: {platformOptions.Transport}");
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            var platformOptions = app.ApplicationServices.GetRequiredService<IOptions<PlatformOptions>>().Value;

            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = HealthCheckResponseWriter
            });
            app.UseHealthChecks("/health/live", new HealthCheckOptions {ResponseWriter = HealthCheckResponseWriter});

            if (!string.IsNullOrWhiteSpace(platformOptions.Prometheus))
            {
                Log.Information("Configuring Prometheus Endpoint: /metrics");

                app.UseMetricServer();
            }
        }

        static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
        {
            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(entry => new JProperty(entry.Key, new JObject(
                    new JProperty("status", entry.Value.Status.ToString()),
                    new JProperty("description", entry.Value.Description),
                    new JProperty("data", JObject.FromObject(entry.Value.Data))))))));

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
