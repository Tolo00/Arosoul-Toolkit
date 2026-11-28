using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// Converts console text to command arguments and property values.
    /// Built in: string, char, bool, all numeric types, enums, Vector2/3/4, Vector2Int/3Int, Quaternion, Color and Color32.
    /// Add your own types with <see cref="ValueParser{T}"/>.
    /// </summary>
    /// <remarks>
    /// Formats: bool true/false/1/0/yes/no/on/off. Enums by name (case-insensitive, dashes ignored), number or flags joined with | or ,.
    /// Vectors as (1, 2, 3), 1,2,3 or, after '=', 1 2 3. Quaternions as Euler angles (x, y, z) or (x, y, z, w).
    /// Colors as hex (#FF8800, #FF8800CC, FF8800) or a Unity color name (red, cyan, orange…).
    /// </remarks>
    public static class ValueParsers
    {
        private const NumberStyles IntegerStyle = NumberStyles.Integer;
        private const NumberStyles FloatStyle = NumberStyles.Float;

        private static readonly char[] _componentSeparators = { ',', ' ', '\t' };
        private static readonly Dictionary<Type, IValueParser> _customParsers = new();
        private static bool _customParsersLoaded;

        private static CultureInfo Culture => CultureInfo.InvariantCulture;

        /// <summary>
        /// Register a parser, replacing any parser for the same type. Parsers in project assemblies are registered automatically.
        /// </summary>
        public static void Register(IValueParser parser)
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));

            EnsureCustomParsers();
            _customParsers[parser.TargetType] = parser;
        }

        public static bool Unregister(Type type)
        {
            EnsureCustomParsers();
            return type != null && _customParsers.Remove(type);
        }

        public static bool CanParse(Type type)
        {
            if (type == null) return false;

            EnsureCustomParsers();
            if (_customParsers.ContainsKey(type)) return true;

            return type == typeof(string) ||
                   type == typeof(bool) ||
                   type == typeof(char) ||
                   type.IsEnum ||
                   IsNumeric(type) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector4) ||
                   type == typeof(Vector2Int) ||
                   type == typeof(Vector3Int) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Color32);
        }

        public static bool TryParse(string text, Type type, out object value)
        {
            value = null;
            if (type == null) return false;

            text = text?.Trim() ?? string.Empty;
            EnsureCustomParsers();

            if (_customParsers.TryGetValue(type, out IValueParser parser))
                return parser.TryParse(Unquote(text), out value);

            if (type == typeof(string))
            {
                value = Unquote(text);
                return true;
            }

            if (type == typeof(bool)) return TryParseBool(text, out value);
            if (type.IsEnum) return TryParseEnum(Unquote(text), type, out value);
            if (type == typeof(char)) return TryParseChar(Unquote(text), out value);
            if (IsNumeric(type)) return TryParseNumber(text, type, out value);
            if (type == typeof(Color) || type == typeof(Color32)) return TryParseColor(Unquote(text), type, out value);
            return TryParseVector(text, type, out value);
        }

        /// <summary>
        /// Remove surrounding double quotes and unescape \" and \\.
        /// </summary>
        public static string Unquote(string text)
        {
            if (text == null || text.Length < 2 || text[0] != '"' || text[text.Length - 1] != '"') return text;
            return text.Substring(1, text.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        internal static void EnsureCustomParsers()
        {
            if (_customParsersLoaded) return;
            _customParsersLoaded = true;

            foreach (Type type in CommandAssemblyScanner.GetTypes())
            {
                if (type.IsAbstract || type.ContainsGenericParameters || !typeof(IValueParser).IsAssignableFrom(type)) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"Value parser {type.FullName} needs a parameterless constructor to be registered automatically.");
                    continue;
                }

                var parser = (IValueParser)Activator.CreateInstance(type);
                _customParsers[parser.TargetType] = parser;
            }
        }

        #region Primitives

        private static bool IsNumeric(Type type)
        {
            if (type.IsEnum) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseNumber(string text, Type type, out object value)
        {
            value = null;
            bool parsed;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte: { parsed = sbyte.TryParse(text, IntegerStyle, Culture, out sbyte v); value = v; break; }
                case TypeCode.Byte: { parsed = byte.TryParse(text, IntegerStyle, Culture, out byte v); value = v; break; }
                case TypeCode.Int16: { parsed = short.TryParse(text, IntegerStyle, Culture, out short v); value = v; break; }
                case TypeCode.UInt16: { parsed = ushort.TryParse(text, IntegerStyle, Culture, out ushort v); value = v; break; }
                case TypeCode.Int32: { parsed = int.TryParse(text, IntegerStyle, Culture, out int v); value = v; break; }
                case TypeCode.UInt32: { parsed = uint.TryParse(text, IntegerStyle, Culture, out uint v); value = v; break; }
                case TypeCode.Int64: { parsed = long.TryParse(text, IntegerStyle, Culture, out long v); value = v; break; }
                case TypeCode.UInt64: { parsed = ulong.TryParse(text, IntegerStyle, Culture, out ulong v); value = v; break; }
                case TypeCode.Single: { parsed = TryParseFloat(text, out float v); value = v; break; }
                case TypeCode.Double: { parsed = double.TryParse(TrimFloatSuffix(text), FloatStyle, Culture, out double v); value = v; break; }
                case TypeCode.Decimal: { parsed = decimal.TryParse(TrimFloatSuffix(text), FloatStyle, Culture, out decimal v); value = v; break; }
                default: parsed = false; break;
            }

            if (!parsed) value = null;
            return parsed;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(TrimFloatSuffix(text), FloatStyle, Culture, out value);
        }

        private static string TrimFloatSuffix(string text)
        {
            if (text.Length > 1 && (text[text.Length - 1] == 'f' || text[text.Length - 1] == 'F' || text[text.Length - 1] == 'd' || text[text.Length - 1] == 'm'))
                return text.Substring(0, text.Length - 1);
            return text;
        }

        private static bool TryParseBool(string text, out object value)
        {
            value = null;
            switch (text.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseChar(string text, out object value)
        {
            value = null;
            if (text.Length != 1) return false;

            value = text[0];
            return true;
        }

        private static bool TryParseEnum(string text, Type type, out object value)
        {
            value = null;
            if (text.Length == 0) return false;

            string[] parts = text.Split('|', ',');
            if (parts.Length == 1) return TryParseEnumPart(text, type, out value);

            // Flags: combine the parts
            long combined = 0;
            foreach (string part in parts)
            {
                if (!TryParseEnumPart(part.Trim(), type, out object partValue)) return false;
                combined |= Convert.ToInt64(partValue, Culture);
            }

            value = Enum.ToObject(type, combined);
            return true;
        }

        private static bool TryParseEnumPart(string part, Type type, out object value)
        {
            value = null;
            if (part.Length == 0) return false;

            if (long.TryParse(part, IntegerStyle, Culture, out long number))
            {
                value = Enum.ToObject(type, number);
                return true;
            }

            string compactPart = Compact(part);
            foreach (string name in Enum.GetNames(type))
            {
                if (string.Equals(name, part, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Compact(name), compactPart, StringComparison.OrdinalIgnoreCase))
                {
                    value = Enum.Parse(type, name);
                    return true;
                }
            }
            return false;
        }

        private static string Compact(string text) => text.Replace("-", string.Empty).Replace("_", string.Empty);

        #endregion

        #region Unity types

        private static bool TryParseColor(string text, Type type, out object value)
        {
            value = null;
            if (!TryParseColor(text, out Color color)) return false;

            value = type == typeof(Color32) ? (object)(Color32)color : color;
            return true;
        }

        private static bool TryParseColor(string text, out Color color)
        {
            color = default;
            if (text.Length == 0) return false;
            if (ColorUtility.TryParseHtmlString(text.ToLowerInvariant(), out color)) return true;

            bool bareHex = text[0] != '#' && (text.Length == 3 || text.Length == 4 || text.Length == 6 || text.Length == 8) && IsHex(text);
            return bareHex && ColorUtility.TryParseHtmlString($"#{text}", out color);
        }

        private static bool IsHex(string text)
        {
            foreach (char c in text)
            {
                if (!Uri.IsHexDigit(c)) return false;
            }
            return true;
        }

        private static bool TryParseVector(string text, Type type, out object value)
        {
            value = null;
            if (type == typeof(Vector2Int) || type == typeof(Vector3Int))
            {
                int intCount = type == typeof(Vector2Int) ? 2 : 3;
                if (!TryParseComponents(text, intCount, out string[] intParts)) return false;

                var ints = new int[intCount];
                for (int i = 0; i < intCount; i++)
                {
                    if (!int.TryParse(intParts[i], IntegerStyle, Culture, out ints[i])) return false;
                }
                value = intCount == 2 ? (object)new Vector2Int(ints[0], ints[1]) : new Vector3Int(ints[0], ints[1], ints[2]);
                return true;
            }

            if (type == typeof(Quaternion))
            {
                if (TryParseFloats(text, 3, out float[] euler))
                {
                    value = Quaternion.Euler(euler[0], euler[1], euler[2]);
                    return true;
                }
                if (TryParseFloats(text, 4, out float[] q))
                {
                    value = new Quaternion(q[0], q[1], q[2], q[3]);
                    return true;
                }
                return false;
            }

            int count = type == typeof(Vector2) ? 2 : type == typeof(Vector3) ? 3 : type == typeof(Vector4) ? 4 : 0;
            if (count == 0 || !TryParseFloats(text, count, out float[] f)) return false;

            value = count switch
            {
                2 => new Vector2(f[0], f[1]),
                3 => new Vector3(f[0], f[1], f[2]),
                _ => (object)new Vector4(f[0], f[1], f[2], f[3])
            };
            return true;
        }

        private static bool TryParseFloats(string text, int count, out float[] values)
        {
            values = null;
            if (!TryParseComponents(text, count, out string[] parts)) return false;

            values = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (!TryParseFloat(parts[i], out values[i])) return false;
            }
            return true;
        }

        private static bool TryParseComponents(string text, int count, out string[] parts)
        {
            string inner = text.Trim();
            if (inner.Length >= 2 && ((inner[0] == '(' && inner[inner.Length - 1] == ')') || (inner[0] == '[' && inner[inner.Length - 1] == ']')))
                inner = inner.Substring(1, inner.Length - 2);

            parts = inner.Split(_componentSeparators, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == count;
        }

        #endregion
    }
}
