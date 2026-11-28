using System;

namespace Arosoul.Commands
{
    /// <summary>
    /// Adds a prefix in front of every command declared in the class, for example "player.weapon" turns
    /// a damage property into "player.weapon.damage". Prefixes of nested classes are combined with the prefixes of their outer classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class CommandPrefixAttribute : Attribute
    {
        public CommandPrefixAttribute(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }
    }
}
