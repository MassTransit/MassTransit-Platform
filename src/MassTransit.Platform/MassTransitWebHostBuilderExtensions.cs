namespace MassTransit.Platform
{
    using System;
    using Abstractions;
    using Metadata;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;


    public static class MassTransitWebHostBuilderExtensions
    {
        /// <summary>
        /// Configure the MassTransit Platform with a startup class, which is used to add consumers,
        /// sagas, activities, etc. as well as customize the bus configuration if necessary.
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="T">The startup class type</typeparam>
        /// <returns></returns>
        public static IWebHostBuilder UseMassTransitStartup<T>(this IWebHostBuilder builder)
            where T : class, IPlatformStartup
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables("MT_");
            });

            builder.UseSerilog();

            Log.Information("Adding Startup: {StartupType}", TypeMetadataCache<T>.ShortName);
            builder.ConfigureServices(services => services.AddSingleton<IPlatformStartup, T>());

            builder.UseStartup<MassTransitStartup>();

            return builder;
        }

        /// <summary>
        /// Configure the MassTransit Platform with a startup class, which is used to add consumers,
        /// sagas, activities, etc. as well as customize the bus configuration if necessary.
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="T1">The startup class type</typeparam>
        /// <typeparam name="T2">The startup class type</typeparam>
        /// <returns></returns>
        public static IWebHostBuilder UseMassTransitStartup<T1, T2>(this IWebHostBuilder builder)
            where T1 : class, IPlatformStartup
            where T2 : class, IPlatformStartup
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables("MT_");
            });

            builder.UseSerilog();

            builder.ConfigureServices(services =>
            {
                Log.Information("Adding Startup: {StartupType}", TypeMetadataCache<T1>.ShortName);
                services.AddSingleton<IPlatformStartup, T1>();

                Log.Information("Adding Startup: {StartupType}", TypeMetadataCache<T2>.ShortName);
                services.AddSingleton<IPlatformStartup, T2>();
            });

            builder.UseStartup<MassTransitStartup>();

            return builder;
        }

        /// <summary>
        /// Configure the MassTransit Platform with a startup class, which is used to add consumers,
        /// sagas, activities, etc. as well as customize the bus configuration if necessary.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="startupTypes">The startup class types</param>
        /// <returns></returns>
        public static IWebHostBuilder UseMassTransitStartup(this IWebHostBuilder builder, params Type[] startupTypes)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables("MT_");
            });

            builder.UseSerilog();

            builder.ConfigureServices(services =>
            {
                foreach (var type in startupTypes)
                {
                    Log.Information("Adding Startup: {StartupType}", TypeMetadataCache.GetShortName(type));

                    services.AddSingleton(typeof(IPlatformStartup), type);
                }
            });

            builder.UseStartup<MassTransitStartup>();

            return builder;
        }
    }
}