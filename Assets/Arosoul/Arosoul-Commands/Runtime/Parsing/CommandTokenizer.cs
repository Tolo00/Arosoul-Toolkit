using System.Collections.Generic;
using System.Text;

namespace Arosoul.Commands
{
    /// <summary>
    /// Splits console input into a command name and arguments.
    /// Arguments are separated by whitespace. Double quotes group text with spaces ("big goblin", \" escapes a quote),
    /// and parentheses or brackets group vectors and colors: spawn goblin (1, 2, 0).
    /// </summary>
    public static class CommandTokenizer
    {
        /// <summary>
        /// Split input into tokens. The first token is the command name. Quotes around a token are removed, parentheses are kept.
        /// </summary>
        public static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(input)) return tokens;

            var current = new StringBuilder();
            bool inQuotes = false;
            bool hasToken = false;
            int depth = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (inQuotes)
                {
                    if (c == '\\' && i + 1 < input.Length && (input[i + 1] == '"' || input[i + 1] == '\\'))
                    {
                        current.Append(input[++i]);
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                        if (depth > 0) current.Append(c);
                    }
                    else
                    {
                        current.Append(c);
                    }
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    hasToken = true;
                    if (depth > 0) current.Append(c);
                }
                else if (IsOpening(c))
                {
                    depth++;
                    hasToken = true;
                    current.Append(c);
                }
                else if (IsClosing(c))
                {
                    if (depth > 0) depth--;
                    hasToken = true;
                    current.Append(c);
                }
                else if (char.IsWhiteSpace(c) && depth == 0)
                {
                    Flush(tokens, current, ref hasToken);
                }
                else
                {
                    hasToken = true;
                    current.Append(c);
                }
            }

            Flush(tokens, current, ref hasToken);
            return tokens;
        }

        /// <summary>
        /// Split "name = value" input. The name must be a single word; the value is everything after the first top-level '='.
        /// </summary>
        public static bool TrySplitAssignment(string input, out string name, out string valueText)
        {
            name = null;
            valueText = null;
            if (string.IsNullOrEmpty(input)) return false;

            bool inQuotes = false;
            int depth = 0;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"') inQuotes = !inQuotes;
                if (inQuotes) continue;

                if (IsOpening(c)) depth++;
                else if (IsClosing(c) && depth > 0) depth--;
                else if (c == '=' && depth == 0)
                {
                    string left = input.Substring(0, i).Trim();
                    if (left.Length == 0 || ContainsWhiteSpace(left)) return false;

                    name = left;
                    valueText = input.Substring(i + 1).Trim();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The command name at the start of the input: the text before the first whitespace or '='.
        /// </summary>
        public static string GetCommandName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            string trimmed = input.TrimStart();
            int end = 0;
            while (end < trimmed.Length && !char.IsWhiteSpace(trimmed[end]) && trimmed[end] != '=') end++;
            return trimmed.Substring(0, end);
        }

        private static bool IsOpening(char c) => c == '(' || c == '[' || c == '{';

        private static bool IsClosing(char c) => c == ')' || c == ']' || c == '}';

        private static bool ContainsWhiteSpace(string text)
        {
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) return true;
            }
            return false;
        }

        private static void Flush(List<string> tokens, StringBuilder current, ref bool hasToken)
        {
            if (!hasToken) return;

            tokens.Add(current.ToString());
            current.Clear();
            hasToken = false;
        }
    }
}
