namespace MassTransit.Platform
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Serilog;
    using Serilog.Events;


    public static class MassTransitHost
    {
        public static IHostBuilder CreateBuilder(string appPath, string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var builder = new HostBuilder();

            var currentDirectory = Directory.GetCurrentDirectory();

            builder.UseContentRoot(string.IsNullOrWhiteSpace(appPath)
                ? currentDirectory
                : appPath);

            builder.ConfigureHostConfiguration(config =>
            {
                var baseSettingsPath = Path.Combine(currentDirectory, "appsettings.json");
                config.AddJsonFile(baseSettingsPath, true, true);

                config.AddEnvironmentVariables("MT_");
                config.AddEnvironmentVariables();

                if (args != null)
                    config.AddCommandLine(args);
            });

            builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    var envSettingsPath = Path.Combine(currentDirectory, $"appsettings.{env.EnvironmentName}.json");
                    config.AddJsonFile(envSettingsPath, true, true);

                    if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                    {
                        var appAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(env.ApplicationName));
                        if (appAssembly != null)
                            config.AddUserSecrets(appAssembly, true);
                    }
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

        static bool? _isRunningInContainer;

        public static bool IsRunningInContainer =>
            _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;
    }
}
