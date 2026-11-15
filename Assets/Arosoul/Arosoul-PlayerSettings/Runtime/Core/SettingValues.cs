using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Conversion and validation helpers. Values are always handled in a canonical form:
    /// String/Enum => string, Int => int, Float => float, Bool => bool.
    /// </summary>
    public static class SettingValues {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Serialize(SettingType type, object value) {
            switch (type) {
                case SettingType.Float: return Convert.ToSingle(value, Inv).ToString("R", Inv);
                case SettingType.Int: return Convert.ToInt32(value, Inv).ToString(Inv);
                case SettingType.Bool: return Convert.ToBoolean(value, Inv) ? "true" : "false";
                default: return Convert.ToString(value, Inv) ?? string.Empty;
            }
        }

        public static bool TryParse(SettingType type, string text, out object value) {
            value = null;
            if (text == null) return false;

            switch (type) {
                case SettingType.Float:
                    if (float.TryParse(text, NumberStyles.Float, Inv, out var f)) { value = f; return true; }
                    return false;
                case SettingType.Int:
                    if (int.TryParse(text, NumberStyles.Integer, Inv, out var i)) { value = i; return true; }
                    return false;
                case SettingType.Bool:
                    if (bool.TryParse(text, out var b)) { value = b; return true; }
                    return false;
                default:
                    value = text;
                    return true;
            }
        }

        /// <summary>
        /// Converts any reasonable input (boxed int for a float setting, System.Enum for an enum setting...)
        /// to the canonical value, clamped to range / validated against the options.
        /// </summary>
        public static bool TryCoerce(SettingDefinition def, object raw, out object result) {
            result = null;
            if (raw == null) return false;

            try {
                switch (def.Type) {
                    case SettingType.Float: {
                        float f = Convert.ToSingle(raw, Inv);
                        if (float.IsNaN(f) || float.IsInfinity(f)) return false;
                        result = Mathf.Clamp(f, def.Min, def.Max);
                        return true;
                    }
                    case SettingType.Int: {
                        int i = raw is float rf ? Mathf.RoundToInt(rf) : Convert.ToInt32(raw, Inv);
                        result = Mathf.Clamp(i, Mathf.RoundToInt(def.Min), Mathf.RoundToInt(def.Max));
                        return true;
                    }
                    case SettingType.Bool:
                        result = Convert.ToBoolean(raw, Inv);
                        return true;
                    case SettingType.Enum: {
                        string s = Convert.ToString(raw, Inv);
                        if (IndexOf(def.Options, s) < 0) return false;
                        result = s;
                        return true;
                    }
                    default:
                        result = Convert.ToString(raw, Inv) ?? string.Empty;
                        return true;
                }
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is OverflowException) {
                return false;
            }
        }

        /// <summary>Both values must already be canonical.</summary>
        public static bool AreEqual(SettingType type, object a, object b) {
            switch (type) {
                case SettingType.Float: return Mathf.Approximately((float)a, (float)b);
                case SettingType.Int: return (int)a == (int)b;
                case SettingType.Bool: return (bool)a == (bool)b;
                default: return string.Equals((string)a, (string)b, StringComparison.Ordinal);
            }
        }

        public static int IndexOf(IReadOnlyList<string> list, string value) {
            if (list == null) return -1;
            for (int i = 0; i < list.Count; i++) {
                if (string.Equals(list[i], value, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        /// <summary>"FullScreenWindow" => "Full Screen Window", "frame_rate" => "Frame rate".</summary>
        public static string Nicify(string id) {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            var sb = new StringBuilder(id.Length + 4);
            for (int i = 0; i < id.Length; i++) {
                char c = id[i];
                if (c == '_' || c == '-') { sb.Append(' '); continue; }

                bool boundary = i > 0 && char.IsUpper(c) && (char.IsLower(id[i - 1]) || char.IsDigit(id[i - 1]));
                if (boundary) sb.Append(' ');
                sb.Append(sb.Length == 0 ? char.ToUpperInvariant(c) : c);
            }
            return sb.ToString();
        }
    }

}
