namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using Context;


    public class AssemblyFinder
    {
        public delegate bool AssemblyFilter(string filename);


        public delegate void AssemblyLoadFailure(string assemblyName, Exception exception);


        public static IEnumerable<Assembly> FindAssemblies(string assemblyPath, AssemblyLoadFailure loadFailure, bool includeExeFiles, AssemblyFilter filter)
        {
            LogContext.Debug?.Log("Scanning assembly directory: {Path}", assemblyPath);

            IEnumerable<string> dllFiles = Directory.EnumerateFiles(assemblyPath, "*.dll", SearchOption.AllDirectories).ToList();
            IEnumerable<string> files = dllFiles;

            if (includeExeFiles)
            {
                IEnumerable<string> exeFiles = Directory.EnumerateFiles(assemblyPath, "*.exe", SearchOption.AllDirectories).ToList();
                files = dllFiles.Concat(exeFiles);
            }

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var filterName = Path.GetFileName(file);
                if (!filter(filterName))
                {
                    LogContext.Debug?.Log("Filtered assembly: {File}", file);

                    continue;
                }

                Assembly assembly = null;
                try
                {
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
                catch (BadImageFormatException exception)
                {
                    LogContext.Warning?.Log(exception, "Assembly Scan failed: {Name}", name);

                    continue;
                }

                if (assembly != null)
                    yield return assembly;
            }
        }
    }
}