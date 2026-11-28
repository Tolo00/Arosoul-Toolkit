using System;
using System.Collections.Generic;
using System.Linq;

namespace Arosoul.Commands
{
    /// <summary>
    /// Short, C#-style type names for signatures and messages (int, float, List&lt;string&gt;).
    /// </summary>
    public static class TypeNames
    {
        private static readonly Dictionary<Type, string> _aliases = new()
        {
            { typeof(void), "void" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(string), "string" },
            { typeof(object), "object" }
        };

        public static string GetFriendlyName(Type type)
        {
            if (type == null) return "null";
            if (_aliases.TryGetValue(type, out string alias)) return alias;
            if (type.IsArray) return $"{GetFriendlyName(type.GetElementType())}[]";

            if (type.IsGenericType)
            {
                string name = type.Name;
                int tick = name.IndexOf('`');
                if (tick >= 0) name = name.Substring(0, tick);
                return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName))}>";
            }
            return type.Name;
        }
    }
}
