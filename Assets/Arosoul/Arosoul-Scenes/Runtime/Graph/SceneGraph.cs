using System;
using System.Collections.Generic;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Dependency graph of the registered scenes. Pure logic, no Unity scene APIs.
    /// The core scene (if enabled) is an implicit dependency of every other scene.
    /// </summary>
    public sealed class SceneGraph
    {
        private readonly Dictionary<string, List<string>> _dependencies = new Dictionary<string, List<string>>();
        private readonly string _coreGuid;

        public SceneGraph(ScenesData data)
        {
            _coreGuid = data.ActiveCoreGuid;
            for (int i = 0; i < data.Scenes.Count; i++)
            {
                var definition = data.Scenes[i];
                var list = new List<string>();
                for (int d = 0; d < definition.Dependencies.Count; d++)
                {
                    var dependency = definition.Dependencies[d];
                    if (dependency.IsValid) list.Add(dependency.Guid);
                }
                _dependencies[definition.Scene.Guid] = list;
            }
        }

        /// <summary>
        /// Build a graph from raw nodes. Mainly useful for tests.
        /// </summary>
        public SceneGraph(IEnumerable<KeyValuePair<string, string[]>> nodes, string coreGuid)
        {
            _coreGuid = string.IsNullOrEmpty(coreGuid) ? null : coreGuid;
            foreach (var node in nodes)
                _dependencies[node.Key] = new List<string>(node.Value);
        }

        /// <summary>
        /// Guid of the core scene, or null if core is disabled.
        /// </summary>
        public string CoreGuid => _coreGuid;

        public bool Contains(string guid)
        {
            return _dependencies.ContainsKey(guid) || guid == _coreGuid;
        }

        /// <summary>
        /// Direct dependencies of a scene, including the implicit core dependency.
        /// </summary>
        public List<string> GetDependencies(string guid)
        {
            var result = new List<string>();
            if (_coreGuid != null && guid != _coreGuid)
                result.Add(_coreGuid);

            if (_dependencies.TryGetValue(guid, out List<string> explicitDependencies))
            {
                for (int i = 0; i < explicitDependencies.Count; i++)
                {
                    string dependency = explicitDependencies[i];
                    if (dependency != guid && !result.Contains(dependency))
                        result.Add(dependency);
                }
            }
            return result;
        }

        /// <summary>
        /// All scenes that directly depend on a scene.
        /// </summary>
        public List<string> GetDependents(string guid)
        {
            var result = new List<string>();
            foreach (var key in _dependencies.Keys)
                if (GetDependencies(key).Contains(guid)) result.Add(key);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>
        /// Resolve the order in which scenes must be loaded to satisfy all dependencies of the roots.
        /// Dependencies come first. The roots themselves are included. Each scene appears once.
        /// </summary>
        /// <returns>False if a cycle was found. The cycle is returned as a list of guids, starting and ending with the same scene.</returns>
        public bool TryResolveLoadOrder(IEnumerable<string> roots, out List<string> order, out List<string> cycle)
        {
            order = new List<string>();
            cycle = null;

            var state = new Dictionary<string, int>(); // 1 = visiting, 2 = done
            var stack = new List<string>();

            foreach (string root in roots)
            {
                if (!Visit(root, state, stack, order, out cycle))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Same as <see cref="TryResolveLoadOrder"/>, but throws if there is a cycle.
        /// </summary>
        public List<string> ResolveLoadOrder(IEnumerable<string> roots)
        {
            if (!TryResolveLoadOrder(roots, out List<string> order, out List<string> cycle))
                throw new InvalidOperationException("Scene dependency cycle: " + string.Join(" -> ", cycle));
            return order;
        }

        private bool Visit(string guid, Dictionary<string, int> state, List<string> stack, List<string> order, out List<string> cycle)
        {
            cycle = null;
            state.TryGetValue(guid, out int current);
            if (current == 2) return true;
            if (current == 1)
            {
                int start = stack.IndexOf(guid);
                cycle = stack.GetRange(start, stack.Count - start);
                cycle.Add(guid);
                return false;
            }

            state[guid] = 1;
            stack.Add(guid);

            foreach (string dependency in GetDependencies(guid))
            {
                if (!Visit(dependency, state, stack, order, out cycle))
                    return false;
            }

            stack.RemoveAt(stack.Count - 1);
            state[guid] = 2;
            order.Add(guid);
            return true;
        }

        /// <summary>
        /// Find all registered scenes that take part in a dependency cycle. Each cycle is reported once.
        /// </summary>
        public List<List<string>> FindCycles()
        {
            var cycles = new List<List<string>>();
            var seen = new HashSet<string>();
            var keys = new List<string>(_dependencies.Keys);
            keys.Sort(StringComparer.Ordinal);

            foreach (string key in keys)
            {
                if (!TryResolveLoadOrder(new[] { key }, out _, out List<string> cycle))
                {
                    // Identify the cycle by its sorted members so it is only reported once.
                    var members = new List<string>(cycle);
                    members.RemoveAt(members.Count - 1);
                    members.Sort(StringComparer.Ordinal);
                    if (seen.Add(string.Join("|", members)))
                        cycles.Add(cycle);
                }
            }
            return cycles;
        }
    }
}
