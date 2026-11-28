using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assemblies;

namespace Arosoul.Commands
{
    /// <summary>
    /// Finds the types that can declare commands: every type in assemblies that reference Arosoul.Commands, and Arosoul.Commands itself.
    /// Test assemblies (those that reference NUnit) are skipped so test fixtures never show up in the console.
    /// </summary>
    internal static class CommandAssemblyScanner
    {
        private const string CommandsAssemblyName = "Arosoul.Commands";
        private const string TestFrameworkAssemblyName = "nunit.framework";

        private static List<Type> _types;

        public static IReadOnlyList<Type> GetTypes()
        {
            if (_types != null) return _types;

            _types = new List<Type>();
            foreach (Assembly assembly in CurrentAssemblies.GetLoadedAssemblies())
            {
                if (ShouldScan(assembly)) _types.AddRange(GetLoadableTypes(assembly));
            }
            return _types;
        }

        private static bool ShouldScan(Assembly assembly)
        {
            if (assembly.IsDynamic) return false;
            if (assembly.GetName().Name == CommandsAssemblyName) return true;

            bool referencesCommands = false;
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == TestFrameworkAssemblyName) return false;
                if (reference.Name == CommandsAssemblyName) referencesCommands = true;
            }
            return referencesCommands;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return Array.FindAll(e.Types, t => t != null);
            }
        }
    }
}
