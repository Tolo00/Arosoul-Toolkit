using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// Turns members marked with <see cref="CommandAttribute"/> into <see cref="CommandInfo"/>s and reports setup problems.
    /// </summary>
    internal static class CommandDiscovery
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                 BindingFlags.Static | BindingFlags.Instance |
                                                 BindingFlags.DeclaredOnly;

        public static void Discover(IEnumerable<Type> types, List<CommandInfo> commands, List<CommandIssue> issues)
        {
            foreach (Type type in types)
            {
                if (type == null) continue;

                MemberInfo[] members;
                try
                {
                    members = type.GetMembers(MemberFlags);
                }
                catch (Exception)
                {
                    continue; // types that fail to load can not declare usable commands
                }

                foreach (MemberInfo member in members)
                {
                    CommandAttribute attribute = GetAttribute(member);
                    if (attribute == null) continue;

                    CommandInfo command = TryCreate(type, member, attribute, issues);
                    if (command != null) commands.Add(command);
                }
            }

            CheckConflicts(commands, issues);
        }

        /// <summary>
        /// The prefix of a type, combined with the prefixes of the types it is nested in.
        /// </summary>
        public static string GetPrefix(Type type)
        {
            var parts = new List<string>();
            for (Type t = type; t != null; t = t.DeclaringType)
            {
                var attribute = t.GetCustomAttribute<CommandPrefixAttribute>(false);
                if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Prefix))
                    parts.Insert(0, attribute.Prefix.Trim().Trim('.'));
            }
            return string.Join(".", parts);
        }

        private static CommandAttribute GetAttribute(MemberInfo member)
        {
            try
            {
                return member.GetCustomAttribute<CommandAttribute>(false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static CommandInfo TryCreate(Type type, MemberInfo member, CommandAttribute attribute, List<CommandIssue> issues)
        {
            string memberPath = $"{TypeNames.GetFriendlyName(type)}.{member.Name}";
            string localName = string.IsNullOrWhiteSpace(attribute.Name) ? CommandNaming.ToKebabCase(member.Name) : attribute.Name.Trim();
            string name = CommandNaming.Combine(GetPrefix(type), localName);

            if (!CommandNaming.IsValidName(name))
                return Fail(issues, member, $"'{memberPath}' has the invalid command name '{name}'. Use letters, digits, '.', '-' and '_' only.");

            if (type.ContainsGenericParameters)
                return Fail(issues, member, $"'{memberPath}' is declared on the generic type {TypeNames.GetFriendlyName(type)}. Declare commands on non-generic types.");

            bool isStatic;
            bool canWrite = false;
            switch (member)
            {
                case MethodInfo method:
                    if (method.ContainsGenericParameters)
                        return Fail(issues, member, $"'{memberPath}' is a generic method. Commands can not be generic.");

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        if (parameter.ParameterType.IsByRef)
                            return Fail(issues, member, $"Parameter '{parameter.Name}' of '{memberPath}' is ref, in or out. Command parameters must be passed by value.");

                        if (!ValueParsers.CanParse(parameter.ParameterType))
                        {
                            string typeName = TypeNames.GetFriendlyName(parameter.ParameterType);
                            return Fail(issues, member, $"Parameter '{parameter.Name}' of '{memberPath}' has the unsupported type {typeName}. Add a ValueParser<{typeName}> to support it.");
                        }
                    }
                    isStatic = method.IsStatic;
                    break;

                case PropertyInfo property:
                    if (property.GetIndexParameters().Length > 0)
                        return Fail(issues, member, $"'{memberPath}' is an indexer. Indexers can not be commands.");

                    isStatic = (property.GetMethod ?? property.SetMethod).IsStatic;
                    if (property.SetMethod != null)
                        canWrite = CheckWritable(property.PropertyType, memberPath, member, issues);
                    break;

                case FieldInfo field:
                    isStatic = field.IsStatic;
                    if (!field.IsInitOnly && !field.IsLiteral)
                        canWrite = CheckWritable(field.FieldType, memberPath, member, issues);
                    break;

                default:
                    return null;
            }

            if (!isStatic && !typeof(Component).IsAssignableFrom(type))
                return Fail(issues, member, $"'{memberPath}' is an instance member of {TypeNames.GetFriendlyName(type)}, which is not a Component. Make it static or declare it on a MonoBehaviour.");

            return new CommandInfo(name, attribute.Description, member, attribute.MonoTarget, canWrite);
        }

        private static bool CheckWritable(Type valueType, string memberPath, MemberInfo member, List<CommandIssue> issues)
        {
            if (ValueParsers.CanParse(valueType)) return true;

            string typeName = TypeNames.GetFriendlyName(valueType);
            issues.Add(new CommandIssue(CommandIssueSeverity.Warning, $"'{memberPath}' can be read but not set, its type {typeName} is not supported. Add a ValueParser<{typeName}> to support it.", member));
            return false;
        }

        private static void CheckConflicts(List<CommandInfo> commands, List<CommandIssue> issues)
        {
            foreach (IGrouping<string, CommandInfo> group in commands.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                List<CommandInfo> values = group.Where(c => !c.IsMethod).ToList();
                List<CommandInfo> methods = group.Where(c => c.IsMethod).ToList();

                if (values.Count > 1)
                    issues.Add(new CommandIssue(CommandIssueSeverity.Error, $"'{group.Key}' is declared by several properties or fields ({JoinPaths(values)}). Only {values[0].MemberPath} is used.", null));

                if (values.Count > 0 && methods.Count > 0)
                    issues.Add(new CommandIssue(CommandIssueSeverity.Error, $"'{group.Key}' is both a value ({JoinPaths(values)}) and a method ({JoinPaths(methods)}). Only the value can be used; give one of them another name.", null));

                foreach (IGrouping<string, CommandInfo> sameParameters in methods.GroupBy(GetParameterKey).Where(g => g.Count() > 1))
                    issues.Add(new CommandIssue(CommandIssueSeverity.Warning, $"'{group.Key}' has several overloads with the same parameters ({JoinPaths(sameParameters)}). Only the first one can be reached.", null));
            }
        }

        private static string GetParameterKey(CommandInfo command)
        {
            return string.Join(",", command.Parameters.Select(p => p.ParameterType.FullName));
        }

        private static string JoinPaths(IEnumerable<CommandInfo> commands)
        {
            return string.Join(", ", commands.Select(c => c.MemberPath));
        }

        private static CommandInfo Fail(List<CommandIssue> issues, MemberInfo member, string message)
        {
            issues.Add(new CommandIssue(CommandIssueSeverity.Error, message, member));
            return null;
        }
    }
}
