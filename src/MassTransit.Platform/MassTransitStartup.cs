namespace MassTransit.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Configuration;
    using Definition;
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

            var platformStartups = services.BuildServiceProvider().GetService<IEnumerable<IPlatformStartup>>()?.ToList();

            ConfigureApplicationInsights(services);

            services.TryAddSingleton(KebabCaseEndpointNameFormatter.Instance);
            services.AddMassTransit(cfg =>
            {
                foreach (var platformStartup in platformStartups)
                    platformStartup.ConfigureMassTransit(cfg, services);

                cfg.AddBus(CreateBus);
            });

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(2);
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

        IBusControl CreateBus(IRegistrationContext<IServiceProvider> context)
        {
            var platformOptions = context.Container.GetRequiredService<IOptions<PlatformOptions>>().Value;

            var configurator = new StartupBusConfigurator(platformOptions);

            switch (platformOptions.Transport.ToLower(CultureInfo.InvariantCulture))
            {
                case PlatformOptions.RabbitMq:
                case PlatformOptions.RMQ:
                    return new RabbitMqStartupBusFactory().CreateBus(context, configurator);

                case PlatformOptions.AzureServiceBus:
                case PlatformOptions.ASB:
                    return new ServiceBusStartupBusFactory().CreateBus(context, configurator);

                case PlatformOptions.ActiveMq:
                case PlatformOptions.AMQ:
                    return new ActiveMqStartupBusFactory().CreateBus(context, configurator);

                case PlatformOptions.AmazonSqs:
                    return new AmazonSqsStartupBusFactory().CreateBus(context, configurator);

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
            context.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));

            return context.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
