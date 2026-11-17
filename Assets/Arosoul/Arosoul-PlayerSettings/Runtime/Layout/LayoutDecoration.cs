using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public enum DecorationParameterKind { Text, Number }

    /// <summary>One editable value of a decoration. The layout editor draws a field for it.</summary>
    public sealed class DecorationParameter {
        public string Key { get; }
        public string Label { get; }
        public DecorationParameterKind Kind { get; }
        public string Default { get; }

        DecorationParameter(string key, string label, DecorationParameterKind kind, string def) {
            Key = key; Label = label; Kind = kind; Default = def;
        }

        public static DecorationParameter Text(string key, string label, string def = "") =>
            new DecorationParameter(key, label, DecorationParameterKind.Text, def);

        public static DecorationParameter Number(string key, string label, float def = 0f) =>
            new DecorationParameter(key, label, DecorationParameterKind.Number, def.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The values of one placed decoration. Unset parameters return their default.</summary>
    public sealed class DecorationContext {
        readonly EntryLayout entry;
        readonly IReadOnlyList<DecorationParameter> parameters;

        public DecorationContext(EntryLayout entry, IReadOnlyList<DecorationParameter> parameters) {
            this.entry = entry;
            this.parameters = parameters;
        }

        public string GetText(string key) {
            string value = entry.GetParameter(key);
            if (value != null) return value;
            foreach (var p in parameters) if (p.Key == key) return p.Default;
            return string.Empty;
        }

        public float GetNumber(string key) {
            float.TryParse(GetText(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float result);
            return result;
        }
    }

    /// <summary>
    /// Pure UI decoration in a settings category (header, separator, spacing...). Derive from this (public, parameterless
    /// constructor), decorations are discovered automatically and show up in the layout editor's Add menu.
    /// </summary>
    public abstract class LayoutDecoration {
        /// <summary>Unique id, stored in the layout asset. Must not collide with a setting or section id.</summary>
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public virtual string Group => "Decorations";
        public virtual IReadOnlyList<DecorationParameter> Parameters => Array.Empty<DecorationParameter>();

        public abstract VisualElement Build(DecorationContext context);
    }

}