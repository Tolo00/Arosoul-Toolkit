using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Finds concrete subclasses at runtime. In the editor this uses TypeCache. In builds it scans this assembly and
    /// assemblies that reference it (so user code can add settings and displays). link.xml keeps them from being stripped.
    /// </summary>
    internal static class TypeDiscovery {

        public static List<T> CreateInstances<T>() where T : class {
            var result = new List<T>();
            foreach (var type in FindDerived(typeof(T))) {
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                try {
                    result.Add((T)Activator.CreateInstance(type));
                }
                catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            }
            return result;
        }

        static IEnumerable<Type> FindDerived(Type baseType) {
#if UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesDerivedFrom(baseType);
#else
            var found = new List<Type>();
            var thisAssembly = typeof(TypeDiscovery).Assembly;
            string thisName = thisAssembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly != thisAssembly && !assembly.GetReferencedAssemblies().Any(r => r.Name == thisName)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types) {
                    if (type != baseType && baseType.IsAssignableFrom(type)) found.Add(type);
                }
            }
            return found;
#endif
        }
    }

}
