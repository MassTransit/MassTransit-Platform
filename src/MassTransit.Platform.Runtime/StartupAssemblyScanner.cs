namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Abstractions;
    using Util;


    public class StartupAssemblyScanner
    {
        public IEnumerable<AssemblyStartup> GetAssemblyRegistrations(string path)
        {
            var scanner = new RuntimeAssemblyScanner();
            scanner.ExcludeFileNameStartsWith("Microsoft.", "NewId.", "Newtonsoft.", "RabbitMQ.", "sni.dll", "System.", "SQLite.");
            scanner.Include(IsSupportedType);

            if (!string.IsNullOrWhiteSpace(path))
            {
                scanner.ExcludeAssembliesFromBaseDirectory();
                scanner.AssembliesFromPath(path);
            }
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

        static bool IsSupportedType(Type type)
        {
            return type.GetInterfaces().Contains(typeof(IPlatformStartup));
        }
    }
}
