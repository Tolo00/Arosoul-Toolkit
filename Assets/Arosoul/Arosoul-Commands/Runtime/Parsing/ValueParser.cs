using System;

namespace Arosoul.Commands
{
    /// <summary>
    /// Base class for parsers of your own types, so they can be used as command parameters and property values.
    /// Non-abstract subclasses with a parameterless constructor are found and registered automatically.
    /// A parser for a built-in type replaces the built-in parsing.
    /// </summary>
    /// <example>
    /// <code>
    /// public class ItemParser : ValueParser&lt;ItemSO&gt;
    /// {
    ///     public override bool TryParse(string text, out ItemSO value)
    ///     {
    ///         value = ItemDatabase.Find(text);
    ///         return value != null;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class ValueParser<T> : IValueParser
    {
        public Type TargetType => typeof(T);

        /// <param name="text">The argument text, with surrounding quotes removed.</param>
        public abstract bool TryParse(string text, out T value);

        bool IValueParser.TryParse(string text, out object value)
        {
            bool parsed = TryParse(text, out T result);
            value = result;
            return parsed;
        }
    }
}
