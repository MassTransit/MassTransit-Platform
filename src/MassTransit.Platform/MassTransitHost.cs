namespace MassTransit.Platform
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;


    public static class MassTransitHost
    {
        public static IHostBuilder CreateBuilder(string[] args)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables("DOTNET_");
                config.AddEnvironmentVariables("MT_");

                if (args != null)
                    config.AddCommandLine(args);
            });

            builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

                    if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                    {
                        var appAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(env.ApplicationName));
                        if (appAssembly != null)
                            config.AddUserSecrets(appAssembly, true);
                    }

                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .UseSerilog()
                .UseDefaultServiceProvider((context, options) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
                });

            return builder;
        }

        static bool? _isInDocker;

        public static bool IsInDocker =>
            _isInDocker ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;
    }
}
