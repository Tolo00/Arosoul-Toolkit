using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Arosoul.Commands
{
    /// <summary>
    /// A discovered command: a method, property or field marked with <see cref="CommandAttribute"/>.
    /// </summary>
    public sealed class CommandInfo
    {
        private readonly ParameterInfo[] _parameters = Array.Empty<ParameterInfo>();

        internal CommandInfo(string name, string description, MemberInfo member, MonoTargetType monoTarget, bool canWrite)
        {
            Name = name;
            Description = description ?? string.Empty;
            Member = member;
            MonoTarget = monoTarget;

            switch (member)
            {
                case MethodInfo method:
                    Kind = CommandKind.Method;
                    IsStatic = method.IsStatic;
                    ValueType = method.ReturnType;
                    _parameters = method.GetParameters();
                    break;
                case PropertyInfo property:
                    Kind = CommandKind.Property;
                    IsStatic = (property.GetMethod ?? property.SetMethod).IsStatic;
                    ValueType = property.PropertyType;
                    CanRead = property.GetMethod != null;
                    CanWrite = canWrite;
                    break;
                case FieldInfo field:
                    Kind = CommandKind.Field;
                    IsStatic = field.IsStatic;
                    ValueType = field.FieldType;
                    CanRead = true;
                    CanWrite = canWrite;
                    break;
                default:
                    throw new ArgumentException("Commands can only be methods, properties or fields.", nameof(member));
            }

            foreach (ParameterInfo parameter in _parameters)
            {
                if (!parameter.IsOptional) RequiredParameterCount++;
            }

            ArgumentSignature = BuildArgumentSignature();
        }

        /// <summary>
        /// Full command name including the class prefix, for example "player.weapon.damage".
        /// </summary>
        public string Name { get; }

        public string Description { get; }
        public CommandKind Kind { get; }
        public MemberInfo Member { get; }
        public Type DeclaringType => Member.DeclaringType;
        public bool IsStatic { get; }

        /// <summary>
        /// Which instances an instance command runs on. Not used by static commands.
        /// </summary>
        public MonoTargetType MonoTarget { get; }

        public bool IsMethod => Kind == CommandKind.Method;
        public IReadOnlyList<ParameterInfo> Parameters => _parameters;
        public int RequiredParameterCount { get; }

        /// <summary>
        /// Return type of a method, or the value type of a property or field.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// True for properties and fields whose value can be printed. Always false for methods.
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        /// True for properties and fields that can be set with "name = value". Always false for methods.
        /// </summary>
        public bool CanWrite { get; }

        /// <summary>
        /// Everything after the name, for example "&lt;string enemy&gt; [int count = 1]" or "= &lt;float&gt;".
        /// </summary>
        public string ArgumentSignature { get; }

        public string Signature => ArgumentSignature.Length == 0 ? Name : $"{Name} {ArgumentSignature}";

        /// <summary>
        /// Where the command is declared, for example "PlayerWeapon.Damage".
        /// </summary>
        public string MemberPath => $"{TypeNames.GetFriendlyName(DeclaringType)}.{Member.Name}";

        public string TargetDescription => IsStatic ? "static" : $"instance ({MonoTarget})";

        public override string ToString() => Signature;

        internal object GetValue(object target)
        {
            return Member is PropertyInfo property ? property.GetValue(target) : ((FieldInfo)Member).GetValue(target);
        }

        internal void SetValue(object target, object value)
        {
            if (Member is PropertyInfo property)
                property.SetValue(target, value);
            else
                ((FieldInfo)Member).SetValue(target, value);
        }

        internal object Invoke(object target, object[] arguments)
        {
            return ((MethodInfo)Member).Invoke(target, arguments);
        }

        private string BuildArgumentSignature()
        {
            string typeName = TypeNames.GetFriendlyName(ValueType);
            if (Kind != CommandKind.Method)
            {
                if (CanRead && CanWrite) return $"= <{typeName}>";
                return CanRead ? $": {typeName} (read-only)" : $"= <{typeName}> (write-only)";
            }

            var sb = new StringBuilder();
            foreach (ParameterInfo parameter in _parameters)
            {
                if (sb.Length > 0) sb.Append(' ');

                string parameterType = TypeNames.GetFriendlyName(parameter.ParameterType);
                if (parameter.IsOptional)
                {
                    bool isStructDefault = parameter.DefaultValue == null && parameter.ParameterType.IsValueType;
                    string defaultValue = parameter.HasDefaultValue && !isStructDefault ? ValueFormatter.Format(parameter.DefaultValue) : "default";
                    sb.Append($"[{parameterType} {parameter.Name} = {defaultValue}]");
                }
                else
                {
                    sb.Append($"<{parameterType} {parameter.Name}>");
                }
            }
            return sb.ToString();
        }
    }
}
