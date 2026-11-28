using System.Text;

namespace Arosoul.Commands
{
    /// <summary>
    /// Builds command names from member names and class prefixes.
    /// </summary>
    public static class CommandNaming
    {
        /// <summary>
        /// Convert a member name to kebab-case: SetDamage becomes set-damage, _maxSpeed becomes max-speed, FPSCounter becomes fps-counter.
        /// Leading underscores and m_, s_ and k_ prefixes are removed.
        /// </summary>
        public static string ToKebabCase(string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return string.Empty;

            string name = memberName;
            if (name.Length > 2 && name[1] == '_' && (name[0] == 'm' || name[0] == 's' || name[0] == 'k'))
                name = name.Substring(2);
            name = name.TrimStart('_');

            var sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || c == '-' || char.IsWhiteSpace(c))
                {
                    AppendSeparator(sb);
                    continue;
                }

                if (char.IsUpper(c) && i > 0)
                {
                    char previous = name[i - 1];
                    bool afterLowerOrDigit = char.IsLower(previous) || char.IsDigit(previous);
                    bool endOfAcronym = char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1]);
                    if (afterLowerOrDigit || endOfAcronym) AppendSeparator(sb);
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString().Trim('-');
        }

        /// <summary>
        /// Join a prefix and a name with a dot. An empty prefix returns the name.
        /// </summary>
        public static string Combine(string prefix, string name)
        {
            prefix = prefix?.Trim().Trim('.');
            return string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
        }

        /// <summary>
        /// True if the name only contains letters, digits, '.', '-' and '_', and does not start or end with a dot.
        /// </summary>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || name[0] == '.' || name[name.Length - 1] == '.') return false;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_') return false;
            }
            return true;
        }

        private static void AppendSeparator(StringBuilder sb)
        {
            if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
        }
    }
}
