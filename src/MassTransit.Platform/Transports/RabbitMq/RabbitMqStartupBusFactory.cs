namespace MassTransit.Platform.Transports.RabbitMq
{
    using System;
    using System.Net.Security;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Serilog;


    public class RabbitMqStartupBusFactory :
        IStartupBusFactory
    {
        public IBusControl CreateBus(IRegistrationContext<IServiceProvider> context, IStartupBusConfigurator configurator)
        {
            var options = context.Container.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var sslOptions = context.Container.GetRequiredService<IOptions<RabbitMqSslOptions>>().Value;

            return Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(options.Host, options.Port, options.VHost, h =>
                {
                    h.Username(options.User);
                    h.Password(options.Pass);

                    if (options.UseSsl)
                    {
                        h.UseSsl(s =>
                        {
                            s.ServerName = sslOptions.ServerName;
                            s.CertificatePath = sslOptions.CertPath;
                            s.CertificatePassphrase = sslOptions.CertPassphrase;
                            s.UseCertificateAsAuthenticationIdentity = sslOptions.CertIdentity;

                            if (sslOptions.Trust)
                            {
                                s.AllowPolicyErrors(SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors
                                    | SslPolicyErrors.RemoteCertificateNotAvailable);
                            }
                        });
                    }
                });

                if (!configurator.TryConfigureQuartz(cfg))
                {
                    Log.Information("Configuring RabbitMQ Message Scheduler (delayed exchange)");
                    cfg.UseDelayedExchangeMessageScheduler();
                }

                configurator.ConfigureBus(cfg, context);
            });
        }

        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqOptions>(configuration.GetSection("RMQ"));
            services.Configure<RabbitMqSslOptions>(configuration.GetSection("RMQ").GetSection("SSL"));
        }
    }
}
