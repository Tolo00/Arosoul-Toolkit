using System;
using UnityEngine.Scripting;

namespace Arosoul.Commands
{
    /// <summary>
    /// Exposes a method, property or field to the command console.
    /// Static members run without a target. Instance members must be declared on a Component (usually a MonoBehaviour)
    /// and run on the active instances chosen by <see cref="MonoTarget"/>.
    /// Without a name the command is named after the member in kebab-case (SetDamage becomes set-damage),
    /// prefixed by the <see cref="CommandPrefixAttribute"/> of the declaring class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class CommandAttribute : PreserveAttribute
    {
        public CommandAttribute() { }

        /// <param name="name">Command name, used as written. The class prefix is still added in front of it.</param>
        public CommandAttribute(string name)
        {
            Name = name;
        }

        /// <param name="name">Command name, used as written. The class prefix is still added in front of it.</param>
        /// <param name="description">Shown in autocomplete, help and the Commands Debugger.</param>
        public CommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <param name="monoTarget">Which instances an instance command runs on.</param>
        public CommandAttribute(MonoTargetType monoTarget)
        {
            MonoTarget = monoTarget;
        }

        /// <param name="name">Command name, used as written. The class prefix is still added in front of it.</param>
        /// <param name="monoTarget">Which instances an instance command runs on.</param>
        public CommandAttribute(string name, MonoTargetType monoTarget)
        {
            Name = name;
            MonoTarget = monoTarget;
        }

        /// <summary>
        /// Explicit command name, or null to derive it from the member name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Shown in autocomplete, help and the Commands Debugger.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Which active instances an instance command runs on. Ignored for static members.
        /// </summary>
        public MonoTargetType MonoTarget { get; set; } = MonoTargetType.First;
    }
}
