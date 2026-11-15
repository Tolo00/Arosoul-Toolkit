using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace Arosoul.PlayerSettings.EditorTools {

    /// <summary>Writes, reads back and validates the generated CustomSettings.cs file.</summary>
    internal static class CustomSettingsCodeGenerator {
        public const string ClassName = "CustomSettings";
        public const string DefaultFolder = "Assets/Settings";

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static readonly HashSet<string> Reserved = new HashSet<string> { ClassName, "Data", "Register" };

        static readonly HashSet<string> Keywords = new HashSet<string> {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal",
            "default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object",
            "operator","out","override","params","private","protected","public","readonly","ref","return","sbyte","sealed","short",
            "sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof","uint","ulong",
            "unchecked","unsafe","ushort","using","virtual","void","volatile","while"
        };

#region Reading

        /// <summary>Reads the settings back from the compiled generated class (empty if it doesn't exist yet).</summary>
        public static List<CustomSettingData> LoadExisting(out string scriptPath) {
            scriptPath = FindScriptPath();
            var result = new List<CustomSettingData>();

            foreach (var type in TypeCache.GetTypesWithAttribute<GeneratedSettingsAttribute>()) {
                var field = type.GetField("Data", BindingFlags.Public | BindingFlags.Static);
                if (field?.GetValue(null) is CustomSettingData[] data) {
                    result.AddRange(data.Select(d => d.Clone()));
                    break;
                }
            }
            return result;
        }

        static string FindScriptPath() {
            foreach (string guid in AssetDatabase.FindAssets(ClassName + " t:MonoScript")) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != ClassName || !File.Exists(path)) continue;
                if (File.ReadAllText(path).Contains("[GeneratedSettings]")) return path;
            }
            return null;
        }

#endregion

#region Validation

        public static bool IsIdentifier(string s) {
            if (string.IsNullOrEmpty(s) || Keywords.Contains(s)) return false;
            if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
            return s.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        public static List<string> Validate(IReadOnlyList<CustomSettingData> rows) {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var builtInIds = new HashSet<string>(SettingsRegistry.BuiltIn.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);
            var enumTypeNames = new HashSet<string>(rows.Where(r => r.type == SettingType.Enum && IsIdentifier(r.apiName)).Select(r => r.apiName + "Option"));

            for (int i = 0; i < rows.Count; i++) {
                var r = rows[i];
                string who = string.IsNullOrWhiteSpace(r.apiName) ? $"Row {i + 1}" : r.apiName;

                if (!IsIdentifier(r.apiName)) errors.Add($"{who}: API name must be a valid C# identifier.");
                else if (Reserved.Contains(r.apiName)) errors.Add($"{who}: '{r.apiName}' is reserved.");
                else if (!seen.Add(r.apiName)) errors.Add($"{who}: API name is used more than once.");
                else if (builtInIds.Contains(r.apiName)) errors.Add($"{who}: conflicts with a built-in setting id.");
                else if (enumTypeNames.Contains(r.apiName)) errors.Add($"{who}: conflicts with the enum type generated for another setting.");

                switch (r.type) {
                    case SettingType.Int:
                    case SettingType.Float:
                        if (r.min >= r.max) { errors.Add($"{who}: min must be lower than max."); break; }
                        if (!string.IsNullOrWhiteSpace(r.defaultValue)) {
                            if (!SettingValues.TryParse(r.type, r.defaultValue.Trim(), out var n)) errors.Add($"{who}: default is not a valid {r.type.ToString().ToLower()}.");
                            else if (Convert.ToSingle(n, Inv) < r.min || Convert.ToSingle(n, Inv) > r.max) errors.Add($"{who}: default is outside min/max.");
                        }
                        break;

                    case SettingType.Bool:
                        if (!string.IsNullOrWhiteSpace(r.defaultValue) && !SettingValues.TryParse(SettingType.Bool, r.defaultValue.Trim(), out _)) {
                            errors.Add($"{who}: default must be true or false.");
                        }
                        break;

                    case SettingType.Enum: {
                        var options = r.options ?? new string[0];
                        if (options.Length == 0) { errors.Add($"{who}: needs at least one option."); break; }
                        if (options.Any(o => !IsIdentifier(o))) errors.Add($"{who}: options must be valid C# identifiers (letters, digits, underscore).");
                        if (options.Distinct().Count() != options.Length) errors.Add($"{who}: options must be unique.");
                        if (!string.IsNullOrWhiteSpace(r.defaultValue) && Array.IndexOf(options, r.defaultValue.Trim()) < 0) errors.Add($"{who}: default must be one of the options.");
                        break;
                    }
                }
            }
            return errors;
        }

#endregion

#region Generation

        public static string Generate(IReadOnlyList<CustomSettingData> rows) {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     Generated by Arosoul Player Settings (Tools > Arosoul > Player Settings > Custom Settings).");
            sb.AppendLine("//     Changes to this file are overwritten. Edit the settings in the editor window instead.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine("using Arosoul.PlayerSettings;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace Arosoul.PlayerSettings {");
            sb.AppendLine();

            foreach (var r in rows.Where(r => r.type == SettingType.Enum)) {
                sb.AppendLine($"    public enum {r.apiName}Option {{ {string.Join(", ", r.options)} }}");
                sb.AppendLine();
            }

            sb.AppendLine("    [GeneratedSettings]");
            sb.AppendLine($"    public static class {ClassName} {{");
            sb.AppendLine();

            foreach (var r in rows) {
                string type = TypeName(r);
                sb.AppendLine($"        /// <summary>{XmlEscape(DisplayName(r))}</summary>");
                sb.AppendLine($"        public static {type} {r.apiName} {{");
                sb.AppendLine($"            get => GameSettings.Get<{type}>({Literal(r.apiName)});");
                sb.AppendLine($"            set => GameSettings.Set({Literal(r.apiName)}, value);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>Parameters of every setting. The editor window reads this back.</summary>");
            sb.AppendLine("        public static readonly CustomSettingData[] Data = {");
            foreach (var r in rows) {
                sb.Append("            new CustomSettingData { ");
                sb.Append($"apiName = {Literal(r.apiName)}, displayName = {Literal(DisplayName(r))}, type = SettingType.{r.type}, ");
                sb.Append($"min = {FloatLiteral(r.min)}, max = {FloatLiteral(r.max)}, defaultValue = {Literal(NormalizeDefault(r))}");
                if (r.type == SettingType.Enum) sb.Append($", options = new[] {{ {string.Join(", ", r.options.Select(Literal))} }}");
                sb.AppendLine(" },");
            }
            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]");
            sb.AppendLine("        static void Register() => SettingsRegistry.RegisterCustom(Data);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("}");
            return sb.ToString().Replace("\r\n", "\n");
        }

        static string TypeName(CustomSettingData r) {
            switch (r.type) {
                case SettingType.Int: return "int";
                case SettingType.Float: return "float";
                case SettingType.Bool: return "bool";
                case SettingType.Enum: return r.apiName + "Option";
                default: return "string";
            }
        }

        static string DisplayName(CustomSettingData r) {
            string name = string.IsNullOrWhiteSpace(r.displayName) ? SettingValues.Nicify(r.apiName) : r.displayName;
            return name.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        static string NormalizeDefault(CustomSettingData r) {
            string text = r.defaultValue?.Trim() ?? string.Empty;
            if (text.Length == 0) return string.Empty;

            if (r.type == SettingType.Float || r.type == SettingType.Int || r.type == SettingType.Bool) {
                return SettingValues.TryParse(r.type, text, out var v) ? SettingValues.Serialize(r.type, v) : string.Empty;
            }
            return r.type == SettingType.Enum ? text : r.defaultValue;
        }

        static string FloatLiteral(float f) => f.ToString("R", Inv) + "f";

        static string XmlEscape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        static string Literal(string s) {
            if (s == null) return "\"\"";
            var sb = new StringBuilder("\"");
            foreach (char c in s) {
                switch (c) {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }
    }

#endregion

}
