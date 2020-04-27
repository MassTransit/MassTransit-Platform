namespace MassTransit.Platform.Runtime
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;


    static class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            var appPath = Environment.GetEnvironmentVariable("MT_APP");

            return MassTransitHost.CreateBuilder(appPath, args)
                .ConfigureWebHostDefaults(builder =>
                {
                    if (!string.IsNullOrWhiteSpace(appPath) && Directory.Exists(appPath))
                    {
                        builder.ConfigureAppConfiguration((context, config) =>
                        {
                            var env = context.HostingEnvironment.EnvironmentName;

                            config.AddJsonFile(Path.Combine(appPath, "appsettings.json"), true);
                            config.AddJsonFile(Path.Combine(appPath, $"appsettings.{env}.json"), true);
                        });
                    }

                    Type[] startupTypes = new StartupAssemblyScanner()
                        .GetAssemblyRegistrations(appPath)
                        .SelectMany(x => x.Types)
                        .ToArray();

                    builder.UseMassTransitStartup(startupTypes);
                });
        }
    }
}
