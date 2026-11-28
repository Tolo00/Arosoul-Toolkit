using System;
using System.Collections.Generic;

namespace Arosoul.Commands
{
    /// <summary>
    /// Ranks commands against what has been typed: exact match, then prefix, then the start of a dot or dash separated
    /// segment, then substring, then fuzzy (the typed characters in order, "pwd" matches "player.weapon.damage").
    /// </summary>
    public static class CommandAutocomplete
    {
        private const int ExactScore = 1000;
        private const int PrefixScore = 900;
        private const int SegmentScore = 700;
        private const int ContainsScore = 500;
        private const int MaxFuzzyScore = 300;

        /// <summary>
        /// The best matching commands, best first. Overloads are listed separately.
        /// </summary>
        public static List<CommandInfo> GetSuggestions(CommandRegistry registry, string query, int maxResults)
        {
            var results = new List<CommandInfo>();
            if (registry == null || maxResults <= 0 || string.IsNullOrWhiteSpace(query)) return results;

            query = query.Trim();
            var scored = new List<(CommandInfo command, int score)>();
            foreach (CommandInfo command in registry.Commands)
            {
                int score = Score(command.Name, query);
                if (score > 0) scored.Add((command, score));
            }

            scored.Sort((a, b) =>
            {
                if (a.score != b.score) return b.score.CompareTo(a.score);
                if (a.command.Name.Length != b.command.Name.Length) return a.command.Name.Length.CompareTo(b.command.Name.Length);
                int byName = string.Compare(a.command.Name, b.command.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.command.Parameters.Count.CompareTo(b.command.Parameters.Count);
            });

            for (int i = 0; i < scored.Count && i < maxResults; i++)
                results.Add(scored[i].command);
            return results;
        }

        /// <summary>
        /// How well a name matches the query. 0 means no match, higher is better.
        /// </summary>
        public static int Score(string name, string query)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(query)) return 0;
            if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase)) return ExactScore;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return PrefixScore - Math.Min(99, name.Length - query.Length);

            int segment = IndexOfSegmentStart(name, query);
            if (segment > 0) return SegmentScore - Math.Min(99, segment);

            int index = name.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) return ContainsScore - Math.Min(99, index);

            return GetFuzzyScore(name, query);
        }

        private static int IndexOfSegmentStart(string name, string query)
        {
            for (int i = 1; i + query.Length <= name.Length; i++)
            {
                if (IsSeparator(name[i - 1]) && string.Compare(name, i, query, 0, query.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    return i;
            }
            return -1;
        }

        private static int GetFuzzyScore(string name, string query)
        {
            int score = 0;
            int matched = 0;
            int lastMatch = -2;
            for (int i = 0; i < name.Length && matched < query.Length; i++)
            {
                if (char.ToLowerInvariant(name[i]) != char.ToLowerInvariant(query[matched])) continue;

                score += 10;
                if (i == 0 || IsSeparator(name[i - 1])) score += 15;
                if (lastMatch == i - 1) score += 5;
                lastMatch = i;
                matched++;
            }

            if (matched < query.Length) return 0;
            return Math.Clamp(100 + score - name.Length, 1, MaxFuzzyScore);
        }

        private static bool IsSeparator(char c) => c == '.' || c == '-' || c == '_';
    }
}
