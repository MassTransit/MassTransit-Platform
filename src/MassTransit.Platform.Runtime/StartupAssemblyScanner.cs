namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Abstractions;
    using Context;
    using Metadata;
    using Util;


    public class StartupAssemblyScanner
    {
        readonly string _startupTypeName;

        public StartupAssemblyScanner()
        {
            _startupTypeName = TypeMetadataCache<IPlatformStartup>.ShortName;
        }

        public IEnumerable<AssemblyStartup> GetAssemblyRegistrations(string path)
        {
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;

            var scanner = new RuntimeAssemblyScanner();
            scanner.ExcludeFileNameStartsWith("Automatonymous.", "GreenPipes", "MassTransit.", "Microsoft.", "NewId.", "Newtonsoft.",
                "RabbitMQ.", "Serilog.", "sni.dll", "System.", "SQLite.", "Topshelf.");
            scanner.Include(IsSupportedType);

            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                    scanner.AssembliesFromPath(path);
                else
                    scanner.AssembliesFromApplicationBaseDirectory();

                if (scanner.Count == 0)
                    return Enumerable.Empty<AssemblyStartup>();

                var typeSet = scanner.ScanForTypes().Result;

                List<AssemblyStartup> registrations = typeSet.FindTypes(TypeClassification.Concrete | TypeClassification.Closed)
                    .GroupBy(x => x.Assembly)
                    .Select(x => new AssemblyStartup(x.Key, x.ToArray()))
                    .ToList();

                return registrations;
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomain_ReflectionOnlyAssemblyResolve;
            }
        }

        Assembly ReflectionOnlyLoadAssembly(string assemblyFile)
        {
            try
            {
                return Assembly.ReflectionOnlyLoadFrom(assemblyFile);
            }
            catch (BadImageFormatException e)
            {
                LogContext.Warning?.Log(e, "Assembly Scan failed: {File}", assemblyFile);
                return null;
            }
        }

        bool IsSupportedType(Type type)
        {
            foreach (var interfaceType in type.GetInterfaces())
            {
                var name = TypeMetadataCache.GetShortName(interfaceType);

                if (name.Equals(_startupTypeName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
}