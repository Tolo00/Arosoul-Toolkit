using System;

namespace Arosoul.Commands
{
    /// <summary>
    /// Converts console text to a value of <see cref="TargetType"/>. Derive from <see cref="ValueParser{T}"/> instead of implementing this directly.
    /// </summary>
    public interface IValueParser
    {
        Type TargetType { get; }

        bool TryParse(string text, out object value);
    }
}
