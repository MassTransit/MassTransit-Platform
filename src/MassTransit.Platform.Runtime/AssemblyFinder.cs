namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using Abstractions;
    using McMaster.NETCore.Plugins;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;


    public static class AssemblyFinder
    {
        public delegate bool AssemblyFilter(string filename);


        public delegate bool AssemblyTypeFilter(Type type);


        public delegate void AssemblyLoadFailure(string assemblyName, Exception exception);


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

                Assembly assembly = null;
                try
                {
                    if (isPlugIn)
                    {
                        var loader = PluginLoader.CreateFromAssemblyFile(file,
                            sharedTypes: new[] {typeof(IPlatformStartup), typeof(IServiceCollection), typeof(ILogger)},
                            isUnloadable: true, configure: config =>
                            {
                                config.PreferSharedTypes = true;
                            });

                        var defaultAssembly = loader.LoadDefaultAssembly();

                        var hasMatchingType = defaultAssembly.GetExportedTypes().Any(x => typeFilter(x));
                        if (hasMatchingType)
                        {
                            loader.Dispose();

                            loader = PluginLoader.CreateFromAssemblyFile(file,
                                sharedTypes: new[] {typeof(IPlatformStartup), typeof(IServiceCollection), typeof(ILogger)},
                                isUnloadable: false, configure: config =>
                                {
                                    config.PreferSharedTypes = true;
                                });

                            assembly = loader.LoadDefaultAssembly();
                        }
                        else
                        {
                            loader.Dispose();
                        }
                    }
                    else
                    {
                        assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                    }
                }
                catch (BadImageFormatException exception)
                {
                    loadFailure(file, exception);

                    continue;
                }

                if (assembly != null)
                    yield return assembly;
            }
        }
    }
}
