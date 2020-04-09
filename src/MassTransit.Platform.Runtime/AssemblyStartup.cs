namespace MassTransit.Platform.Runtime
{
    using System;
    using System.Diagnostics;
    using System.Reflection;


    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class AssemblyStartup
    {
        public readonly Assembly Assembly;
        public readonly Type[] Types;

        public AssemblyStartup(Assembly assembly, Type[] types)
        {
            Assembly = assembly;
            Types = types;
        }

        string DebuggerDisplay => $"{Assembly.GetName().Name} ( {Types.Length} types)";
    }
}