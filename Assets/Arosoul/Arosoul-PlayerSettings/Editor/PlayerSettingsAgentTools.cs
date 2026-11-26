using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Arosoul.PlayerSettings.EditorTools {

    /// <summary>
    /// Public Editor APIs for authoring custom settings and SettingsLayout assets without driving EditorWindow UI.
    /// Call these methods from an Editor script, Unity batch-mode -executeMethod entry point, or automation bridge.
    /// </summary>
    public static class PlayerSettingsAgentTools {

        /// <summary>Reads the most recently compiled CustomSettings definition and returns cloned rows.</summary>
        public static List<CustomSettingData> LoadCustomSettings(out string existingScriptPath) =>
            CustomSettingsCodeGenerator.LoadExisting(out existingScriptPath);

        /// <summary>Returns the validation errors that must be fixed before custom settings can be generated.</summary>
        public static string[] ValidateCustomSettings(IList<CustomSettingData> rows) {
            if (rows == null) return new[] { "Custom settings list cannot be null." };
            var diagnostics = new List<string>();
            var validRows = new List<CustomSettingData>();
            for (int i = 0; i < rows.Count; i++) {
                if (rows[i] == null) diagnostics.Add($"Row {i + 1}: entry is null.");
                else validRows.Add(rows[i].Clone());
            }
            SettingsRegistry.Rebuild();
            diagnostics.AddRange(CustomSettingsCodeGenerator.Validate(validRows));
            return diagnostics.ToArray();
        }

        /// <summary>
        /// Generates CustomSettings.cs and returns its Unity asset path. Unity must compile the generated file
        /// before new settings become discoverable in the layout catalog; perform layout authoring after compilation.
        /// </summary>
        public static string GenerateCustomSettings(IList<CustomSettingData> rows, string outputFolder = CustomSettingsCodeGenerator.DefaultFolder) {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var copy = rows.Select(r => r != null ? r.Clone() : null).ToList();
            if (copy.Any(r => r == null)) throw new ArgumentException("Custom settings list contains a null entry.", nameof(rows));

            SettingsRegistry.Rebuild();
            var errors = CustomSettingsCodeGenerator.Validate(copy);
            if (errors.Count > 0) throw new ArgumentException("Custom settings validation failed:\n" + string.Join("\n", errors), nameof(rows));

            string folder = NormalizeAssetsFolder(outputFolder);
            string path = folder + "/" + CustomSettingsCodeGenerator.ClassName + ".cs";
            string oldPath;
            CustomSettingsCodeGenerator.LoadExisting(out oldPath);

            Directory.CreateDirectory(folder);
            string normalizedOldPath = string.IsNullOrEmpty(oldPath) ? null : oldPath.Replace('\\', '/');
            bool targetIsCurrentGeneratedScript = string.Equals(normalizedOldPath, path, StringComparison.Ordinal);
            bool targetContainsGeneratedMarker = File.Exists(path) && File.ReadAllText(path).Contains("[GeneratedSettings]");
            if (File.Exists(path) && !targetIsCurrentGeneratedScript && !targetContainsGeneratedMarker) {
                throw new InvalidOperationException($"Refusing to overwrite '{path}' because it is not a recognised generated CustomSettings script.");
            }

            if (!string.IsNullOrEmpty(oldPath) && !targetIsCurrentGeneratedScript) {
                if (!normalizedOldPath.StartsWith("Assets/", StringComparison.Ordinal)) {
                    throw new InvalidOperationException($"Refusing to move generated settings script from outside Assets: '{oldPath}'.");
                }
                if (!AssetDatabase.DeleteAsset(oldPath)) {
                    throw new InvalidOperationException($"Could not remove the previous generated settings script at '{oldPath}'. Refusing to create a duplicate CustomSettings class.");
                }
            }

            CustomSettingsCodeGenerator.Write(path, copy);
            AssetDatabase.SaveAssets();
            return path;
        }

        /// <summary>
        /// Adds a custom setting, or replaces the one with the same apiName, keeping every other setting and the script location.
        /// Safe to call several times before Unity recompiles. Returns the generated script path.
        /// </summary>
        public static string AddOrUpdateCustomSetting(CustomSettingData row) {
            if (row == null) throw new ArgumentNullException(nameof(row));
            var rows = LoadCustomSettings(out string scriptPath);
            int index = rows.FindIndex(r => r.apiName == row.apiName);
            if (index >= 0) rows[index] = row.Clone();
            else rows.Add(row.Clone());
            return GenerateCustomSettings(rows, FolderOf(scriptPath));
        }

        /// <summary>
        /// Removes a custom setting by apiName. Its saved player value is no longer used. Returns false if it does not exist.
        /// Remove its layout entries too (ValidateLayout reports them as unknown ids after compilation).
        /// </summary>
        public static bool RemoveCustomSetting(string apiName) {
            var rows = LoadCustomSettings(out string scriptPath);
            if (rows.RemoveAll(r => r.apiName == apiName) == 0) return false;
            GenerateCustomSettings(rows, FolderOf(scriptPath));
            return true;
        }

        static string FolderOf(string scriptPath) =>
            string.IsNullOrEmpty(scriptPath) ? CustomSettingsCodeGenerator.DefaultFolder : Path.GetDirectoryName(scriptPath).Replace('\\', '/');

        /// <summary>Returns all registered setting, section, and decoration ids for layout authoring.</summary>
        public static string[] GetAvailableItemIds() {
            SettingsRegistry.Rebuild();
            return SettingsRegistry.All.Select(s => s.Id)
                .Concat(SettingsRegistry.Sections.Select(s => s.Id))
                .Concat(SettingsRegistry.Decorations.Select(d => d.Id))
                .ToArray();
        }

        /// <summary>Returns the display ids valid for a setting or section. Decorations do not have a display type.</summary>
        public static string[] GetAvailableDisplayIds(string itemId) {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
            SettingsRegistry.Rebuild();

            if (SettingsRegistry.TryGet(itemId, out var definition)) {
                return SettingDisplays.For(definition.Type).Select(d => d.Id).ToArray();
            }
            if (SettingsRegistry.TryGetSection(itemId, out var section)) {
                return section.Displays.Select(d => d.Id).ToArray();
            }
            if (SettingsRegistry.TryGetDecoration(itemId, out _)) return Array.Empty<string>();
            throw new ArgumentException($"Unknown Player Settings item id '{itemId}'.", nameof(itemId));
        }

        /// <summary>Creates a new SettingsLayout asset with the supplied top-level properties.</summary>
        public static SettingsLayout CreateLayout(
            string assetPath,
            string title = "Settings",
            TabOrientation tabOrientation = TabOrientation.Horizontal,
            bool showCloseButton = true,
            SettingsTheme theme = null) {

            assetPath = NormalizeAssetPath(assetPath);
            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("SettingsLayout asset path must end in .asset.", nameof(assetPath));
            }
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(assetPath)) {
                throw new InvalidOperationException($"An asset already exists at '{assetPath}'.");
            }

            if (!Enum.IsDefined(typeof(TabOrientation), tabOrientation)) throw new ArgumentOutOfRangeException(nameof(tabOrientation));
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.Refresh();

            var layout = ScriptableObject.CreateInstance<SettingsLayout>();
            SetLayoutProperties(layout, title, tabOrientation, showCloseButton, theme, false, false);
            AssetDatabase.CreateAsset(layout, assetPath);
            SaveLayout(layout);
            return layout;
        }

        /// <summary>Updates the top-level properties of an existing SettingsLayout asset.</summary>
        public static void ConfigureLayout(
            SettingsLayout layout,
            string title,
            TabOrientation tabOrientation,
            bool showCloseButton,
            SettingsTheme theme = null) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            SetLayoutProperties(layout, title, tabOrientation, showCloseButton, theme, true);
        }

        /// <summary>Returns the index of the first category with the given name, or -1. Use it to get the categoryIndex other methods take.</summary>
        public static int FindCategoryIndex(SettingsLayout layout, string name) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return layout.Categories == null ? -1 : layout.Categories.FindIndex(c => c != null && c.name == name);
        }

        /// <summary>Readable summary of a layout: properties, then each category with its index, mode and indexed entries.</summary>
        public static string DescribeLayout(SettingsLayout layout) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            var sb = new StringBuilder();
            sb.Append($"Layout '{layout.Title}' path={AssetDatabase.GetAssetPath(layout)} tabs={layout.TabOrientation} close={layout.ShowCloseButton} theme={(layout.Theme != null ? layout.Theme.name : "default")}\n");
            if (layout.Categories == null) return sb.ToString();

            for (int c = 0; c < layout.Categories.Count; c++) {
                var category = layout.Categories[c];
                if (category == null) { sb.Append($"[{c}] (null)\n"); continue; }
                sb.Append($"[{c}] '{category.name}' mode={category.applyMode} reset={category.showResetButton}\n");
                if (category.entries == null) continue;
                for (int e = 0; e < category.entries.Count; e++) {
                    var entry = category.entries[e];
                    if (entry == null) { sb.Append($"    [{e}] (null)\n"); continue; }
                    sb.Append($"    [{e}] {entry.settingId}");
                    if (!string.IsNullOrEmpty(entry.displayType)) sb.Append(" display=").Append(entry.displayType);
                    if (entry.parameters != null) {
                        foreach (var parameter in entry.parameters) {
                            if (parameter != null) sb.Append($" {parameter.key}={parameter.value}");
                        }
                    }
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>Adds a category and returns it so callers can add entries or adjust its fields.</summary>
        public static CategoryLayout AddCategory(
            SettingsLayout layout,
            string name,
            ApplyMode applyMode = ApplyMode.Manual,
            bool showResetButton = true) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.Categories == null) throw new InvalidOperationException("SettingsLayout.Categories is null.");
            if (!Enum.IsDefined(typeof(ApplyMode), applyMode)) throw new ArgumentOutOfRangeException(nameof(applyMode));

            Undo.RecordObject(layout, "Add Settings Category");
            var category = new CategoryLayout {
                name = string.IsNullOrWhiteSpace(name) ? "New Category" : name.Trim(),
                applyMode = applyMode,
                showResetButton = showResetButton
            };
            layout.Categories.Add(category);
            SaveLayout(layout);
            return category;
        }

        /// <summary>Updates an existing category by index. Index is the category's current order in the layout.</summary>
        public static void ConfigureCategory(
            SettingsLayout layout,
            int categoryIndex,
            string name,
            ApplyMode applyMode,
            bool showResetButton) {
            var category = GetCategory(layout, categoryIndex);
            if (!Enum.IsDefined(typeof(ApplyMode), applyMode)) throw new ArgumentOutOfRangeException(nameof(applyMode));
            Undo.RecordObject(layout, "Configure Settings Category");
            category.name = string.IsNullOrWhiteSpace(name) ? "New Category" : name.Trim();
            category.applyMode = applyMode;
            category.showResetButton = showResetButton;
            SaveLayout(layout);
        }

        /// <summary>
        /// Adds one setting, section, or decoration to a category. Repeated entries are allowed.
        /// displayType may be omitted to use the first/default supported display. For a decoration, pass null.
        /// </summary>
        public static EntryLayout AddEntry(
            SettingsLayout layout,
            int categoryIndex,
            string itemId,
            string displayType = null,
            IReadOnlyDictionary<string, string> parameters = null) {

            var category = GetCategory(layout, categoryIndex);
            var entry = BuildEntry(itemId, displayType, parameters);

            Undo.RecordObject(layout, "Add Settings Entry");
            if (category.entries == null) category.entries = new List<EntryLayout>();
            category.entries.Add(entry);
            SaveLayout(layout);
            return entry;
        }

        /// <summary>Replaces an existing entry after validating its item id, display and decoration parameters.</summary>
        public static void ConfigureEntry(
            SettingsLayout layout,
            int categoryIndex,
            int entryIndex,
            string itemId,
            string displayType = null,
            IReadOnlyDictionary<string, string> parameters = null) {

            var category = GetCategory(layout, categoryIndex);
            if (category.entries == null || entryIndex < 0 || entryIndex >= category.entries.Count) {
                throw new ArgumentOutOfRangeException(nameof(entryIndex), "Entry index is outside the category's entry list.");
            }
            var replacement = BuildEntry(itemId, displayType, parameters);
            Undo.RecordObject(layout, "Configure Settings Entry");
            category.entries[entryIndex] = replacement;
            SaveLayout(layout);
        }

        /// <summary>Moves a category to a new ordered index.</summary>
        public static void MoveCategory(SettingsLayout layout, int fromIndex, int toIndex) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.Categories == null) throw new InvalidOperationException("SettingsLayout.Categories is null.");
            if (fromIndex < 0 || fromIndex >= layout.Categories.Count) throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= layout.Categories.Count) throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (fromIndex == toIndex) return;
            Undo.RecordObject(layout, "Move Settings Category");
            var category = layout.Categories[fromIndex];
            layout.Categories.RemoveAt(fromIndex);
            layout.Categories.Insert(toIndex, category);
            SaveLayout(layout);
        }

        /// <summary>Removes a category and all of its layout entries.</summary>
        public static bool RemoveCategory(SettingsLayout layout, int categoryIndex) {
            if (layout == null || layout.Categories == null || categoryIndex < 0 || categoryIndex >= layout.Categories.Count) return false;
            Undo.RecordObject(layout, "Remove Settings Category");
            layout.Categories.RemoveAt(categoryIndex);
            SaveLayout(layout);
            return true;
        }

        /// <summary>Moves an entry to a new ordered index within its existing category.</summary>
        public static void MoveEntry(SettingsLayout layout, int categoryIndex, int fromIndex, int toIndex) {
            var category = GetCategory(layout, categoryIndex);
            if (category.entries == null || fromIndex < 0 || fromIndex >= category.entries.Count) throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= category.entries.Count) throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (fromIndex == toIndex) return;
            Undo.RecordObject(layout, "Move Settings Entry");
            var entry = category.entries[fromIndex];
            category.entries.RemoveAt(fromIndex);
            category.entries.Insert(toIndex, entry);
            SaveLayout(layout);
        }

        /// <summary>Removes the entry at the supplied category and entry indices.</summary>
        public static bool RemoveEntry(SettingsLayout layout, int categoryIndex, int entryIndex) {
            var category = GetCategory(layout, categoryIndex);
            if (category.entries == null || entryIndex < 0 || entryIndex >= category.entries.Count) return false;
            Undo.RecordObject(layout, "Remove Settings Entry");
            category.entries.RemoveAt(entryIndex);
            SaveLayout(layout);
            return true;
        }

        static EntryLayout BuildEntry(string itemId, string displayType, IReadOnlyDictionary<string, string> parameters) {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
            SettingsRegistry.Rebuild();
            var entry = new EntryLayout { settingId = itemId };

            if (SettingsRegistry.TryGet(itemId, out var definition)) {
                var displays = SettingDisplays.For(definition.Type).ToList();
                if (displays.Count == 0) throw new InvalidOperationException($"No SettingDisplay supports '{itemId}' ({definition.Type}).");
                var selected = displayType == null ? displays[0] : displays.FirstOrDefault(d => d.Id == displayType);
                if (selected == null) throw new ArgumentException($"Display '{displayType}' does not support setting '{itemId}' ({definition.Type}).", nameof(displayType));
                entry.displayType = selected.Id;
                if (parameters != null && parameters.Count > 0) throw new ArgumentException("Parameters are only valid for layout decorations.", nameof(parameters));
            }
            else if (SettingsRegistry.TryGetSection(itemId, out var section)) {
                var selected = displayType == null
                    ? section.Displays.FirstOrDefault()
                    : section.Displays.FirstOrDefault(d => d.Id == displayType);
                if (selected == null) throw new ArgumentException($"Display '{displayType}' is not valid for section '{itemId}'.", nameof(displayType));
                entry.displayType = selected.Id;
                if (parameters != null && parameters.Count > 0) throw new ArgumentException("Parameters are only valid for layout decorations.", nameof(parameters));
            }
            else if (SettingsRegistry.TryGetDecoration(itemId, out var decoration)) {
                if (!string.IsNullOrEmpty(displayType)) throw new ArgumentException("Decorations do not use a display type.", nameof(displayType));
                if (parameters != null) {
                    foreach (var pair in parameters) {
                        var parameter = decoration.Parameters.FirstOrDefault(p => p.Key == pair.Key);
                        if (parameter == null) throw new ArgumentException($"Decoration '{itemId}' has no parameter named '{pair.Key}'.", nameof(parameters));
                        string value = pair.Value ?? string.Empty;
                        if (parameter.Kind == DecorationParameterKind.Number
                            && !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
                            throw new ArgumentException($"Decoration parameter '{pair.Key}' must be an invariant-culture number.", nameof(parameters));
                        }
                        entry.SetParameter(pair.Key, value);
                    }
                }
            }
            else {
                throw new ArgumentException($"Unknown Player Settings item id '{itemId}'. Refresh the catalog after generated code has compiled.", nameof(itemId));
            }

            return entry;
        }

        /// <summary>Validates references and display compatibility. Warnings describe runtime fallbacks or platform-specific omissions.</summary>
        public static string[] ValidateLayout(SettingsLayout layout) {
            var diagnostics = new List<string>();
            if (layout == null) return new[] { "ERROR: SettingsLayout is null." };
            SettingsRegistry.Rebuild();

            if (layout.Categories == null) return new[] { "ERROR: SettingsLayout.Categories is null." };
            for (int c = 0; c < layout.Categories.Count; c++) {
                var category = layout.Categories[c];
                if (category == null) {
                    diagnostics.Add($"ERROR: Category at index {c} is null.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(category.name)) diagnostics.Add($"WARNING: Category {c} has no name.");
                if (!Enum.IsDefined(typeof(ApplyMode), category.applyMode)) diagnostics.Add($"ERROR: Category '{category.name}' has an invalid ApplyMode value.");
                if (category.entries == null) {
                    diagnostics.Add($"ERROR: Category '{category.name}' has a null entries list.");
                    continue;
                }

                for (int e = 0; e < category.entries.Count; e++) {
                    var entry = category.entries[e];
                    string location = $"Category '{category.name}', entry {e}";
                    if (entry == null || string.IsNullOrWhiteSpace(entry.settingId)) {
                        diagnostics.Add($"ERROR: {location} has no item id.");
                        continue;
                    }
                    location += $" ('{entry.settingId}')";

                    if (SettingsRegistry.TryGet(entry.settingId, out var definition)) {
                        if (!definition.IsAvailable) diagnostics.Add($"WARNING: '{entry.settingId}' is unavailable on the current platform and will not be shown.");
                        var display = SettingDisplays.Find(entry.displayType);
                        if (display == null || !display.Supports(definition.Type)) {
                            diagnostics.Add($"WARNING: {location} uses unsupported display '{entry.displayType}' for {definition.Type}; runtime will use the default supported display.");
                        }
                        continue;
                    }

                    if (SettingsRegistry.TryGetSection(entry.settingId, out var section)) {
                        if (!section.Displays.Any(d => d.Id == entry.displayType)) {
                            diagnostics.Add($"WARNING: {location} uses unknown display '{entry.displayType}' for section '{entry.settingId}'; runtime will use its first display.");
                        }
                        continue;
                    }

                    if (SettingsRegistry.TryGetDecoration(entry.settingId, out var decoration)) {
                        foreach (var parameter in entry.parameters ?? new List<EntryParameter>()) {
                            if (parameter == null) {
                                diagnostics.Add($"ERROR: {location} contains a null decoration parameter.");
                                continue;
                            }
                            var parameterDefinition = decoration.Parameters.FirstOrDefault(p => p.Key == parameter.key);
                            if (parameterDefinition == null) {
                                diagnostics.Add($"ERROR: {location} has unknown decoration parameter '{parameter.key}'.");
                            }
                            else if (parameterDefinition.Kind == DecorationParameterKind.Number
                                && !float.TryParse(parameter.value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
                                diagnostics.Add($"ERROR: {location} parameter '{parameter.key}' must be an invariant-culture number.");
                            }
                        }
                        continue;
                    }

                    diagnostics.Add($"ERROR: {location} references unknown item id '{entry.settingId}'.");
                }
            }

            return diagnostics.ToArray();
        }

        static void SetLayoutProperties(
            SettingsLayout layout,
            string title,
            TabOrientation tabOrientation,
            bool showCloseButton,
            SettingsTheme theme,
            bool recordUndo,
            bool saveAsset = true) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (recordUndo) Undo.RecordObject(layout, "Configure Settings Layout");

            if (!Enum.IsDefined(typeof(TabOrientation), tabOrientation)) throw new ArgumentOutOfRangeException(nameof(tabOrientation));
            var serialized = new SerializedObject(layout);
            serialized.Update();
            RequireProperty(serialized, "title").stringValue = title ?? "Settings";
            RequireProperty(serialized, "tabOrientation").enumValueIndex = (int)tabOrientation;
            RequireProperty(serialized, "showCloseButton").boolValue = showCloseButton;
            RequireProperty(serialized, "theme").objectReferenceValue = theme;
            serialized.ApplyModifiedProperties();
            if (saveAsset) SaveLayout(layout);
        }

        static CategoryLayout GetCategory(SettingsLayout layout, int categoryIndex) {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.Categories == null || categoryIndex < 0 || categoryIndex >= layout.Categories.Count) {
                throw new ArgumentOutOfRangeException(nameof(categoryIndex), "Category index is outside the layout's category list.");
            }
            if (layout.Categories[categoryIndex] == null) throw new InvalidOperationException($"Category at index {categoryIndex} is null.");
            return layout.Categories[categoryIndex];
        }

        static SerializedProperty RequireProperty(SerializedObject serialized, string name) {
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Could not find serialized SettingsLayout field '{name}'.");
            return property;
        }

        static void SaveLayout(SettingsLayout layout) {
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
        }

        static string NormalizeAssetsFolder(string folder) {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Output folder cannot be empty.", nameof(folder));
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if ((folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                || folder.Split('/').Any(part => part == ".." || part == ".")) {
                throw new ArgumentException("Output folder must be project-relative and inside Assets.", nameof(folder));
            }
            return folder;
        }

        static string NormalizeAssetPath(string path) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Asset path cannot be empty.", nameof(path));
            path = path.Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(part => part == ".." || part == ".")) {
                throw new ArgumentException("Asset path must be project-relative and inside Assets.", nameof(path));
            }
            return path;
        }
    }

}
