using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Arosoul.Commands
{
    /// <summary>
    /// Runs console input against a <see cref="CommandRegistry"/> and writes the result to a <see cref="CommandLog"/>.
    /// </summary>
    /// <remarks>
    /// Input forms:
    /// <list type="bullet">
    /// <item><c>name arg1 arg2</c> calls a method. Overloads are chosen by argument count, then by which arguments parse.</item>
    /// <item><c>name</c> prints the value of a property or field.</item>
    /// <item><c>name = value</c> (or <c>name value</c>) sets a property or field.</item>
    /// </list>
    /// </remarks>
    public sealed class CommandExecutor
    {
        private static readonly object[] _staticTargets = { null };

        private readonly CommandRegistry _registry;
        private readonly CommandLog _log;

        public CommandExecutor(CommandRegistry registry, CommandLog log)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Run the input. The input itself is logged first.
        /// </summary>
        /// <returns>True if the command ran without errors.</returns>
        public bool Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();
            _log.Add(input, CommandLogType.Input);

            try
            {
                return CommandTokenizer.TrySplitAssignment(input, out string name, out string valueText)
                    ? ExecuteAssignment(name, valueText)
                    : ExecuteInvocation(input);
            }
            catch (Exception e)
            {
                Exception error = e is TargetInvocationException { InnerException: not null } ? e.InnerException : e;
                _log.Add($"{error.GetType().Name}: {error.Message}", CommandLogType.Error, error.StackTrace);
                return false;
            }
        }

        #region Values

        private bool ExecuteAssignment(string name, string valueText)
        {
            IReadOnlyList<CommandInfo> matches = _registry.Find(name);
            if (matches.Count == 0) return Fail(GetUnknownCommandMessage(name));

            CommandInfo command = matches.FirstOrDefault(c => !c.IsMethod);
            if (command == null) return Fail($"'{matches[0].Name}' is a method and can not be assigned. Usage: {matches[0].Signature}");
            if (!command.CanWrite) return Fail($"'{command.Name}' is read-only.");

            string typeName = TypeNames.GetFriendlyName(command.ValueType);
            if (valueText.Length == 0) return Fail($"Missing value. Usage: {command.Name} = <{typeName}>");
            if (!ValueParsers.TryParse(valueText, command.ValueType, out object value))
                return Fail($"Could not parse '{valueText}' as {typeName}.{GetEnumHint(command.ValueType)}");

            if (!TryGetTargets(command, out IReadOnlyList<object> targets)) return false;

            foreach (object target in targets)
                command.SetValue(target, value);

            string suffix = targets.Count > 1 ? $" on {targets.Count} objects" : string.Empty;
            _log.Add($"{command.Name} = {ValueFormatter.Format(value)}{suffix}");
            return true;
        }

        private bool ExecuteValue(CommandInfo command, string input, IReadOnlyList<string> arguments)
        {
            // "name value" is a shorthand for "name = value"
            if (arguments.Count > 0)
                return ExecuteAssignment(command.Name, input.Substring(GetNameLength(input)).Trim());

            if (!command.CanRead) return Fail($"'{command.Name}' is write-only.");
            if (!TryGetTargets(command, out IReadOnlyList<object> targets)) return false;

            foreach (object target in targets)
            {
                string value = ValueFormatter.Format(command.GetValue(target));
                _log.Add(targets.Count > 1 ? $"{GetTargetName(target)}: {value}" : value);
            }
            return true;
        }

        #endregion

        #region Methods

        private bool ExecuteInvocation(string input)
        {
            List<string> tokens = CommandTokenizer.Tokenize(input);
            if (tokens.Count == 0) return false;

            string name = tokens[0];
            List<string> arguments = tokens.GetRange(1, tokens.Count - 1);

            IReadOnlyList<CommandInfo> matches = _registry.Find(name);
            if (matches.Count == 0) return Fail(GetUnknownCommandMessage(name));

            CommandInfo value = matches.FirstOrDefault(c => !c.IsMethod);
            if (value != null) return ExecuteValue(value, input, arguments);

            // Exact argument counts first, then let a trailing string parameter take the rest of the input
            string parseError = null;
            foreach (bool joinTrailingText in new[] { false, true })
            {
                foreach (CommandInfo method in matches)
                {
                    if (!TryBindArguments(method, arguments, joinTrailingText, out object[] values, out string error))
                    {
                        parseError ??= error;
                        continue;
                    }
                    return Invoke(method, values);
                }
            }

            string usage = string.Join("\n", matches.Select(m => $"  {m.Signature}"));
            return Fail(parseError ?? $"Wrong number of arguments for '{matches[0].Name}'. Usage:\n{usage}");
        }

        private bool Invoke(CommandInfo method, object[] arguments)
        {
            if (!TryGetTargets(method, out IReadOnlyList<object> targets)) return false;

            bool hasResult = method.ValueType != typeof(void);
            foreach (object target in targets)
            {
                object result = method.Invoke(target, arguments);
                if (!hasResult) continue;

                string formatted = ValueFormatter.Format(result);
                _log.Add(targets.Count > 1 ? $"{GetTargetName(target)}: {formatted}" : formatted);
            }

            if (!hasResult && targets.Count > 1)
                _log.Add($"Ran '{method.Name}' on {targets.Count} objects.");
            return true;
        }

        /// <returns>False if the argument count does not fit (error is null) or an argument does not parse (error is set).</returns>
        private static bool TryBindArguments(CommandInfo method, List<string> arguments, bool joinTrailingText, out object[] values, out string error)
        {
            values = null;
            error = null;
            IReadOnlyList<ParameterInfo> parameters = method.Parameters;

            if (joinTrailingText)
            {
                bool canJoin = parameters.Count > 0 &&
                               arguments.Count > parameters.Count &&
                               parameters[parameters.Count - 1].ParameterType == typeof(string);
                if (!canJoin) return false;

                int last = parameters.Count - 1;
                arguments = arguments.Take(last).Append(string.Join(" ", arguments.Skip(last))).ToList();
            }

            if (arguments.Count < method.RequiredParameterCount || arguments.Count > parameters.Count) return false;

            values = new object[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (i >= arguments.Count)
                {
                    values[i] = GetDefaultValue(parameter);
                    continue;
                }

                if (!ValueParsers.TryParse(arguments[i], parameter.ParameterType, out values[i]))
                {
                    error = $"Could not parse '{arguments[i]}' as {TypeNames.GetFriendlyName(parameter.ParameterType)} for parameter '{parameter.Name}'.{GetEnumHint(parameter.ParameterType)}\nUsage: {method.Signature}";
                    values = null;
                    return false;
                }
            }
            return true;
        }

        private static object GetDefaultValue(ParameterInfo parameter)
        {
            Type type = parameter.ParameterType;
            if (!parameter.HasDefaultValue || parameter.DefaultValue == null)
                return type.IsValueType ? Activator.CreateInstance(type) : null;

            object value = parameter.DefaultValue;
            return type.IsEnum && value.GetType() != type ? Enum.ToObject(type, value) : value;
        }

        #endregion

        #region Targets

        private bool TryGetTargets(CommandInfo command, out IReadOnlyList<object> targets)
        {
            targets = _staticTargets;
            if (command.IsStatic) return true;

            Object[] found;
            if (command.MonoTarget == MonoTargetType.First)
            {
                Object first = Object.FindAnyObjectByType(command.DeclaringType, FindObjectsInactive.Exclude);
                found = first != null ? new[] { first } : Array.Empty<Object>();
            }
            else
            {
                found = Object.FindObjectsByType(command.DeclaringType, FindObjectsInactive.Exclude);
            }

            if (found.Length == 0)
            {
                _log.Add($"No active {TypeNames.GetFriendlyName(command.DeclaringType)} in the loaded scenes, '{command.Name}' was not run.", CommandLogType.Warning);
                return false;
            }

            targets = found;
            return true;
        }

        private static string GetTargetName(object target)
        {
            return target is Object unityObject && unityObject != null ? unityObject.name : "static";
        }

        #endregion

        #region Messages

        private bool Fail(string message)
        {
            _log.Add(message, CommandLogType.Error);
            return false;
        }

        private string GetUnknownCommandMessage(string name)
        {
            List<CommandInfo> closest = CommandAutocomplete.GetSuggestions(_registry, name, 1);
            string hint = closest.Count > 0 ? $" Did you mean '{closest[0].Name}'?" : string.Empty;
            return $"Unknown command '{name}'.{hint} Type 'commands' to list all commands.";
        }

        private static string GetEnumHint(Type type)
        {
            return type.IsEnum ? $" Values: {string.Join(", ", Enum.GetNames(type))}." : string.Empty;
        }

        private static int GetNameLength(string input)
        {
            return input.Length - input.TrimStart().Length + CommandTokenizer.GetCommandName(input).Length;
        }

        #endregion
    }
}
