namespace MassTransit.Platform.Transports.AmazonSqs
{
    using System;
    using Amazon.SimpleNotificationService;
    using Amazon.SQS;
    using ExtensionsDependencyInjectionIntegration;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class AmazonSqsStartupBusFactory :
        IStartupBusFactory
    {
        public void CreateBus(IServiceCollectionBusConfigurator busConfigurator, IStartupBusConfigurator configurator)
        {
            if (!configurator.HasSchedulerEndpoint)
                busConfigurator.AddDelayedMessageScheduler();

            busConfigurator.UsingAmazonSqs((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<AmazonSqsOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.Region))
                {
                    cfg.Host(new UriBuilder("amazonsqs://docker.localhost:4576") {Path = options.Scope}.Uri, h =>
                    {
                        h.AccessKey("admin");
                        h.SecretKey("admin");
                        h.Config(new AmazonSimpleNotificationServiceConfig {ServiceURL = "http://docker.localhost:4575"});
                        h.Config(new AmazonSQSConfig {ServiceURL = "http://docker.localhost:4576"});
                    });
                }
                else
                {
                    cfg.Host(new UriBuilder("amazonsqs://host")
                    {
                        Host = options.Region,
                        Path = options.Scope
                    }.Uri, h =>
                    {
                        h.AccessKey(options.AccessKey);
                        h.SecretKey(options.SecretKey);
                    });
                }

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring Amazon SQS Message Scheduler");
                    cfg.UseDelayedMessageScheduler();
                }

                configurator.ConfigureBus(cfg, context);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AmazonSqsOptions>(configuration.GetSection("SQS"));
        }
    }
}
