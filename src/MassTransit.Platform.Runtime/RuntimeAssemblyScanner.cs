namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Serilog;
    using Util;
    using Util.Scanning;


    public class RuntimeAssemblyScanner
    {
        readonly List<Assembly> _assemblies = new List<Assembly>();
        readonly CompositeFilter<string> _assemblyFilter = new CompositeFilter<string>();
        readonly CompositeFilter<Type> _filter = new CompositeFilter<Type>();

        public int Count => _assemblies.Count;

        void Assembly(Assembly assembly)
        {
            if (!_assemblies.Contains(assembly))
                _assemblies.Add(assembly);
        }

        public void Include(Func<Type, bool> predicate)
        {
            _filter.Includes += predicate;
        }

        public void AssembliesFromApplicationBaseDirectory()
        {
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

            IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(assemblyPath, OnAssemblyLoadFailure, false, _assemblyFilter.Matches, _filter
                .Matches);

            foreach (var assembly in assemblies)
                Assembly(assembly);
        }

        public void AssembliesFromPath(string path)
        {
            IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, true, _assemblyFilter.Matches, _filter
                .Matches);

            foreach (var assembly in assemblies)
                Assembly(assembly);
        }

        public void ExcludeAssembliesFromBaseDirectory()
        {
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

            IEnumerable<string> dllFiles = Directory.EnumerateFiles(assemblyPath, "*.dll", SearchOption.AllDirectories).ToList();
            IEnumerable<string> files = dllFiles;

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(file)))
                    continue;

                var fileName = Path.GetFileName(file);

                _assemblyFilter.Excludes += name => name.Equals(fileName, StringComparison.OrdinalIgnoreCase);
            }
        }

        public void ExcludeFileNameStartsWith(params string[] startsWith)
        {
            for (var i = 0; i < startsWith.Length; i++)
            {
                var value = startsWith[i];

                _assemblyFilter.Excludes += name => name.StartsWith(value, StringComparison.OrdinalIgnoreCase);
            }
        }

        static void OnAssemblyLoadFailure(string assemblyName, Exception exception)
        {
            Log.Error(exception, "MassTransit Platform failed to load assembly: {AssemblyName}", assemblyName);
        }

        public Task<TypeSet> ScanForTypes()
        {
            return AssemblyTypeCache.FindTypes(_assemblies, _filter.Matches);
        }
    }
}
