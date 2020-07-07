namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using Abstractions;
    using ActiveMqTransport;
    using AmazonSqsTransport;
    using Azure.ServiceBus.Core;
    using ExtensionsDependencyInjectionIntegration;
    using McMaster.NETCore.Plugins.Loader;
    using Microsoft.Extensions.DependencyInjection;
    using RabbitMqTransport;
    using Serilog;


    public static class AssemblyFinder
    {
        public delegate bool AssemblyFilter(string filename);


        public delegate void AssemblyLoadFailure(string assemblyName, Exception exception);


        public delegate bool AssemblyTypeFilter(Type type);


        public static IEnumerable<Assembly> FindAssemblies(string assemblyPath, AssemblyLoadFailure loadFailure, bool isPlugIn, AssemblyFilter filter,
            AssemblyTypeFilter typeFilter)
        {
            Log.Debug("Scanning assembly directory: {Path}", assemblyPath);

            IEnumerable<string> dllFiles = Directory.EnumerateFiles(assemblyPath, "*.dll", SearchOption.AllDirectories).ToList();
            IEnumerable<string> files = dllFiles;

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var filterName = Path.GetFileName(file);
                if (!filter(filterName))
                    continue;

                var depsPath = Path.Combine(assemblyPath, $"{name}.deps.json");
                if (!File.Exists(depsPath))
                    continue;

                Assembly assembly = null;
                try
                {
                    if (isPlugIn)
                    {
                        var context = GetAssemblyLoadContext(file, depsPath, true);

                        var defaultAssembly = context.LoadFromAssemblyPath(file);

                        var hasMatchingType = defaultAssembly.GetExportedTypes().Any(x => typeFilter(x));

                        context.Unload();

                        if (hasMatchingType)
                        {
                            context = GetAssemblyLoadContext(file, depsPath, false);

                            assembly = context.LoadFromAssemblyPath(file);
                        }
                    }
                    else
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException exception)
                {
                    loadFailure(file, exception);

                    continue;
                }
                catch (FileNotFoundException exception)
                {
                    loadFailure(file, exception);

                    continue;
                }

                if (assembly != null)
                    yield return assembly;
            }
        }

        static AssemblyLoadContext GetAssemblyLoadContext(string assemblyPath, string depsPath, bool enableUnloading)
        {
            var builder = new AssemblyLoadContextBuilder()
                .SetMainAssemblyPath(assemblyPath)
                .PreferDefaultLoadContext(true)
                .AddDependencyContext(depsPath)
                .PreferDefaultLoadContextAssembly(typeof(IPlatformStartup).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IServiceCollection).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IServiceCollectionBusConfigurator).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(ILogger).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IBus).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IRabbitMqBusFactoryConfigurator).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IActiveMqBusFactoryConfigurator).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IAmazonSqsBusFactoryConfigurator).Assembly.GetName())
                .PreferDefaultLoadContextAssembly(typeof(IServiceBusBusFactoryConfigurator).Assembly.GetName());

            if (enableUnloading)
                builder = builder.EnableUnloading();

            return builder.Build();
        }
    }
}
