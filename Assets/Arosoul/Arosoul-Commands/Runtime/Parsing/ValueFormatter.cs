using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// Formats values for console output, in the same syntax the parsers accept.
    /// </summary>
    public static class ValueFormatter
    {
        private const int MaxCollectionItems = 32;

        public static string Format(object value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string text:
                    return text;
                case bool boolean:
                    return boolean ? "true" : "false";
                case float number:
                    return FormatFloat(number);
                case double number:
                    return number.ToString("0.#####", CultureInfo.InvariantCulture);
                case Vector2 v:
                    return $"({FormatFloat(v.x)}, {FormatFloat(v.y)})";
                case Vector3 v:
                    return $"({FormatFloat(v.x)}, {FormatFloat(v.y)}, {FormatFloat(v.z)})";
                case Vector4 v:
                    return $"({FormatFloat(v.x)}, {FormatFloat(v.y)}, {FormatFloat(v.z)}, {FormatFloat(v.w)})";
                case Vector2Int v:
                    return $"({v.x}, {v.y})";
                case Vector3Int v:
                    return $"({v.x}, {v.y}, {v.z})";
                case Quaternion q:
                    return Format(q.eulerAngles);
                case Color color:
                    return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                case Color32 color:
                    return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                case Enum enumValue:
                    return enumValue.ToString();
                case UnityEngine.Object unityObject:
                    return unityObject != null ? unityObject.name : "null";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                case IEnumerable collection:
                    return FormatCollection(collection);
                default:
                    return value.ToString();
            }
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static string FormatCollection(IEnumerable collection)
        {
            var sb = new StringBuilder("[");
            int count = 0;
            foreach (object item in collection)
            {
                if (count > 0) sb.Append(", ");
                if (count == MaxCollectionItems)
                {
                    sb.Append('…');
                    break;
                }
                sb.Append(Format(item));
                count++;
            }
            return sb.Append(']').ToString();
        }
    }
}
