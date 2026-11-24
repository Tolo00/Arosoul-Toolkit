using System.Collections.Generic;
using System.Text;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Turns arbitrary names into valid, unique C# identifiers for generated code.
    /// </summary>
    public static class IdentifierUtility
    {
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        /// <summary>
        /// "Wipe Left" becomes "WipeLeft", "2D fade" becomes "_2DFade". Never returns an empty string.
        /// </summary>
        public static string Sanitize(string name, string fallback = "Unnamed")
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;

            var builder = new StringBuilder(name.Length);
            bool capitalizeNext = false;
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    builder.Append(capitalizeNext && builder.Length > 0 ? char.ToUpperInvariant(c) : c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            if (builder.Length == 0) return fallback;

            string result = builder.ToString();
            if (char.IsDigit(result[0]) || Keywords.Contains(result))
                result = "_" + result;
            return result;
        }

        /// <summary>
        /// Sanitizes all names and makes them unique by appending _2, _3 and so on. Order is preserved.
        /// </summary>
        public static List<string> MakeUnique(IReadOnlyList<string> names)
        {
            var result = new List<string>(names.Count);
            var used = new HashSet<string>();
            for (int i = 0; i < names.Count; i++)
            {
                string baseName = Sanitize(names[i]);
                string candidate = baseName;
                int suffix = 2;
                while (!used.Add(candidate))
                {
                    candidate = baseName + "_" + suffix;
                    suffix++;
                }
                result.Add(candidate);
            }
            return result;
        }
    }
}
