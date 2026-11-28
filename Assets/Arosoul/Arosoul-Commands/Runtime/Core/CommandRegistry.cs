using System;
using System.Collections.Generic;

namespace Arosoul.Commands
{
    /// <summary>
    /// A set of discovered commands, looked up by name (case-insensitive).
    /// <see cref="Default"/> holds every command in the project; tests can build a registry from specific types.
    /// </summary>
    public sealed class CommandRegistry
    {
        private static CommandRegistry _default;

        private readonly List<CommandInfo> _commands = new();
        private readonly List<CommandIssue> _issues = new();
        private readonly Dictionary<string, List<CommandInfo>> _byName = new(StringComparer.OrdinalIgnoreCase);

        public CommandRegistry(IEnumerable<Type> types)
        {
            CommandDiscovery.Discover(types, _commands, _issues);
            _commands.Sort(CompareCommands);

            foreach (CommandInfo command in _commands)
            {
                if (!_byName.TryGetValue(command.Name, out List<CommandInfo> list))
                {
                    list = new List<CommandInfo>();
                    _byName.Add(command.Name, list);
                }
                list.Add(command);
            }
        }

        /// <summary>
        /// Every command in the project. Built on first use, which scans the assemblies that reference Arosoul.Commands.
        /// </summary>
        public static CommandRegistry Default => _default ??= new CommandRegistry(CommandAssemblyScanner.GetTypes());

        /// <summary>
        /// All commands, ordered by name and then by parameter count.
        /// </summary>
        public IReadOnlyList<CommandInfo> Commands => _commands;

        public IReadOnlyList<CommandIssue> Issues => _issues;

        /// <summary>
        /// Commands with the given name (overloads), ordered by parameter count. Empty if there is none.
        /// </summary>
        public IReadOnlyList<CommandInfo> Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return Array.Empty<CommandInfo>();
            return _byName.TryGetValue(name, out List<CommandInfo> list) ? list : Array.Empty<CommandInfo>();
        }

        public bool Contains(string name) => !string.IsNullOrEmpty(name) && _byName.ContainsKey(name);

        private static int CompareCommands(CommandInfo a, CommandInfo b)
        {
            int byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : a.Parameters.Count.CompareTo(b.Parameters.Count);
        }
    }
}
