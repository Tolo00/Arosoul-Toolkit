using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem.EditorTools {

    /// <summary>
    /// Public Editor APIs for deterministic setup and code generation. These methods can be called from an
    /// Editor script, a Unity batch-mode -executeMethod entry point, or an automation bridge.
    /// </summary>
    public static class GameInputAgentTools {
        public const string DefaultConfigPath = "Assets/Resources/GameInputConfig.asset";
        public const string DefaultGeneratedFolder = "Assets/Generated";

        /// <summary>Returns the first GameInputConfig found in the project. This system expects one config.</summary>
        public static GameInputConfig FindConfig() => GameActionsGenerator.FindConfig();

        /// <summary>
        /// Creates the config at the required Resources root path if one does not already exist.
        /// The Resources filename must remain GameInputConfig.asset because runtime loading uses that fixed name.
        /// </summary>
        public static GameInputConfig CreateConfig(string assetPath = DefaultConfigPath) {
            var existing = FindConfig();
            if (existing != null) {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return existing;
            }

            assetPath = NormalizeAssetPath(assetPath);
            if (!assetPath.EndsWith("/Resources/GameInputConfig.asset", StringComparison.Ordinal)) {
                throw new ArgumentException("GameInputConfig must be saved as GameInputConfig.asset directly inside a Resources folder.", nameof(assetPath));
            }

            var atPath = AssetDatabase.LoadAssetAtPath<GameInputConfig>(assetPath);
            if (atPath != null) return atPath;
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null) {
                throw new InvalidOperationException($"An asset already exists at '{assetPath}'.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.Refresh();

            var config = ScriptableObject.CreateInstance<GameInputConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            return config;
        }

        /// <summary>Moves an existing config to the root of a Resources folder and returns the reloaded asset.
        /// Use this when ValidateConfig reports that an older config is outside a Resources root.
        /// </summary>
        public static GameInputConfig MoveConfigToResources(GameInputConfig config, string resourcesFolder = "Assets/Resources") {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(resourcesFolder)) throw new ArgumentException("Resources folder path cannot be empty.", nameof(resourcesFolder));
            resourcesFolder = resourcesFolder.Replace('\\', '/').TrimEnd('/');
            string normalizedProbe = NormalizeAssetPath(resourcesFolder + "/placeholder");
            resourcesFolder = Path.GetDirectoryName(normalizedProbe).Replace('\\', '/');
            if (!string.Equals(Path.GetFileName(resourcesFolder), "Resources", StringComparison.Ordinal)) {
                throw new ArgumentException("The destination folder must itself be named Resources.", nameof(resourcesFolder));
            }
            string targetPath = resourcesFolder + "/GameInputConfig.asset";
            string sourcePath = AssetDatabase.GetAssetPath(config).Replace('\\', '/');
            if (string.IsNullOrEmpty(sourcePath)) throw new InvalidOperationException("GameInputConfig must be saved as an asset before it can be moved.");
            if (string.Equals(sourcePath, targetPath, StringComparison.Ordinal)) return config;
            if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null) throw new InvalidOperationException($"An asset already exists at '{targetPath}'.");

            Directory.CreateDirectory(resourcesFolder);
            AssetDatabase.Refresh();
            string error = AssetDatabase.MoveAsset(sourcePath, targetPath);
            if (!string.IsNullOrEmpty(error)) throw new IOException($"Could not move GameInputConfig: {error}");
            AssetDatabase.Refresh();
            var moved = AssetDatabase.LoadAssetAtPath<GameInputConfig>(targetPath);
            if (moved == null) throw new IOException($"GameInputConfig was moved to '{targetPath}' but could not be reloaded.");
            Selection.activeObject = moved;
            EditorGUIUtility.PingObject(moved);
            return moved;
        }

        /// <summary>
        /// Assigns the persistent InputActionAsset and optionally updates the generation, device, and rebinding settings.
        /// Pass null for optional strings/lists to keep their existing values; pass an empty list to clear exclusions or locks.
        /// </summary>
        public static void ConfigureConfig(
            GameInputConfig config,
            InputActionAsset actions,
            string generatedFolder = null,
            IList<string> excludedMaps = null,
            bool? hideMouseOnGamepadUse = null,
            bool? rumbleEnabled = null,
            string keyboardScheme = null,
            string gamepadScheme = null,
            string keyboardCancelControl = null,
            string gamepadCancelControl = null,
            RebindConflictMode? conflictMode = null,
            IList<string> lockedActions = null,
            bool? autoGenerate = null,
            bool? verboseLogging = null) {

            if (config == null) throw new ArgumentNullException(nameof(config));
            if (actions != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(actions))) {
                throw new ArgumentException("InputActionAsset must be saved as a project asset before assigning it.", nameof(actions));
            }
            if (generatedFolder != null) {
                generatedFolder = generatedFolder.Replace('\\', '/').TrimEnd('/');
                if ((generatedFolder != "Assets" && !generatedFolder.StartsWith("Assets/", StringComparison.Ordinal))
                    || generatedFolder.Split('/').Any(part => part == ".." || part == ".")) {
                    throw new ArgumentException("Generated folder must be project-relative and inside Assets.", nameof(generatedFolder));
                }
            }

            Undo.RecordObject(config, "Configure Game Input");
            var serialized = new SerializedObject(config);
            serialized.Update();

            SetObject(serialized, "actions", actions);
            if (generatedFolder != null) SetString(serialized, "generatedFolder", generatedFolder);
            if (hideMouseOnGamepadUse.HasValue) SetBool(serialized, "hideMouseOnGamepadUse", hideMouseOnGamepadUse.Value);
            if (rumbleEnabled.HasValue) SetBool(serialized, "rumbleEnabled", rumbleEnabled.Value);
            if (keyboardScheme != null) SetString(serialized, "keyboardScheme", keyboardScheme.Trim());
            if (gamepadScheme != null) SetString(serialized, "gamepadScheme", gamepadScheme.Trim());
            if (keyboardCancelControl != null) SetString(serialized, "keyboardCancelControl", keyboardCancelControl.Trim());
            if (gamepadCancelControl != null) SetString(serialized, "gamepadCancelControl", gamepadCancelControl.Trim());
            if (conflictMode.HasValue) {
                if (!Enum.IsDefined(typeof(RebindConflictMode), conflictMode.Value)) throw new ArgumentOutOfRangeException(nameof(conflictMode));
                SetEnum(serialized, "conflictMode", (int)conflictMode.Value);
            }
            if (autoGenerate.HasValue) SetBool(serialized, "autoGenerate", autoGenerate.Value);
            if (verboseLogging.HasValue) SetBool(serialized, "verboseLogging", verboseLogging.Value);
            if (excludedMaps != null) SetStringList(serialized, "excludedMaps", excludedMaps);
            if (lockedActions != null) SetStringList(serialized, "lockedActions", lockedActions);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Returns validation diagnostics. Entries prefixed with ERROR must be fixed; WARNING entries are advisory.</summary>
        public static string[] ValidateConfig(GameInputConfig config = null) {
            var diagnostics = new List<string>();
            config ??= FindConfig();
            if (config == null) return new[] { "ERROR: No GameInputConfig found. Create one with GameInputAgentTools.CreateConfig()." };

            string configPath = AssetDatabase.GetAssetPath(config).Replace('\\', '/');
            if (string.IsNullOrEmpty(configPath)) diagnostics.Add("ERROR: GameInputConfig must be saved as a project asset.");
            const string resourceSuffix = "/Resources/GameInputConfig.asset";
            if (!configPath.EndsWith(resourceSuffix, StringComparison.Ordinal)) {
                diagnostics.Add("ERROR: GameInputConfig must be named GameInputConfig.asset directly inside a Resources folder (not in a Resources subfolder).");
            }

            var configs = AssetDatabase.FindAssets("t:GameInputConfig");
            if (configs.Length > 1) diagnostics.Add($"WARNING: Found {configs.Length} GameInputConfig assets. Runtime Resources loading expects one unambiguous config.");

            if (config.Actions == null) {
                diagnostics.Add("ERROR: No InputActionAsset is assigned to GameInputConfig.actions.");
                return diagnostics.ToArray();
            }

            string generatedFolder = (config.GeneratedFolder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrWhiteSpace(generatedFolder)
                || (generatedFolder != "Assets" && !generatedFolder.StartsWith("Assets/", StringComparison.Ordinal))
                || generatedFolder.Split('/').Any(part => part == ".." || part == ".")) {
                diagnostics.Add("ERROR: Generated Folder must be a project-relative folder inside Assets, for example Assets/Generated.");
            }

            if (config.Actions.actionMaps.Count == 0) diagnostics.Add("WARNING: The assigned InputActionAsset contains no action maps.");

            var mapNames = new HashSet<string>(config.Actions.actionMaps.Select(m => m.name), StringComparer.Ordinal);
            var serializedConfig = new SerializedObject(config);
            var excludedProperty = serializedConfig.FindProperty("excludedMaps");
            if (excludedProperty != null) {
                for (int i = 0; i < excludedProperty.arraySize; i++) {
                    string excluded = excludedProperty.GetArrayElementAtIndex(i).stringValue;
                    if (!mapNames.Contains(excluded)) diagnostics.Add($"WARNING: Excluded map '{excluded}' does not exist in the assigned InputActionAsset.");
                }
            }
            var lockedProperty = serializedConfig.FindProperty("lockedActions");
            if (lockedProperty != null) {
                for (int i = 0; i < lockedProperty.arraySize; i++) {
                    string locked = lockedProperty.GetArrayElementAtIndex(i).stringValue;
                    int slash = locked.IndexOf('/');
                    if (slash <= 0 || slash == locked.Length - 1) {
                        diagnostics.Add($"WARNING: Locked action '{locked}' should use the format Map/Action.");
                        continue;
                    }
                    string mapName = locked.Substring(0, slash);
                    string actionName = locked.Substring(slash + 1);
                    var lockedMap = config.Actions.FindActionMap(mapName, false);
                    if (lockedMap == null || lockedMap.FindAction(actionName, false) == null) diagnostics.Add($"WARNING: Locked action '{locked}' does not match an action in the assigned InputActionAsset.");
                }
            }

            var schemes = config.Actions.controlSchemes.Select(s => s.name).ToArray();
            if (schemes.Length > 0) {
                if (!string.IsNullOrEmpty(config.KeyboardScheme) && !schemes.Contains(config.KeyboardScheme)) diagnostics.Add($"WARNING: Keyboard scheme '{config.KeyboardScheme}' is not defined in the InputActionAsset.");
                if (!string.IsNullOrEmpty(config.GamepadScheme) && !schemes.Contains(config.GamepadScheme)) diagnostics.Add($"WARNING: Gamepad scheme '{config.GamepadScheme}' is not defined in the InputActionAsset.");
            }

            foreach (var map in config.Actions.actionMaps) {
                if (map == null) continue;
                foreach (var action in map.actions) {
                    string expected = action.expectedControlType ?? string.Empty;
                    if (action.name.EndsWith("Priority", StringComparison.Ordinal) && action.name.Length > "Priority".Length) continue;
                    if (expected == "" || expected == "Button" || expected == "Digital"
                        || expected == "Vector2" || expected == "Axis" || expected == "Analog") continue;
                    diagnostics.Add($"WARNING: '{map.name}/{action.name}' has unsupported expected control type '{expected}' and will be omitted from GameActions.cs. Use Button, Vector2, Axis, Analog, or a *Priority action name.");
                }
            }

            return diagnostics.ToArray();
        }

        /// <summary>Generates GameActions.cs for the supplied config and returns its asset path.</summary>
        public static string GenerateActions(GameInputConfig config = null) {
            config ??= FindConfig();
            if (config == null) throw new InvalidOperationException("No GameInputConfig found. Create one before generating actions.");

            string[] diagnostics = ValidateConfig(config);
            string[] errors = diagnostics.Where(d => d.StartsWith("ERROR:", StringComparison.Ordinal)).ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));

            GameActionsGenerator.Generate(config, true);

            string path = (config.GeneratedFolder.TrimEnd('/') + "/GameActions.cs").Replace('\\', '/');
            if (!File.Exists(path)) throw new IOException($"GameActions.cs was not generated at '{path}'. Check the Unity Console for errors.");
            AssetDatabase.Refresh();
            return path;
        }

        /// <summary>Convenient no-argument entry point for Unity -executeMethod.</summary>
        public static void CreateConfigFromCommandLine() => CreateConfig();

        /// <summary>Convenient no-argument entry point for Unity -executeMethod after the InputActionAsset is assigned.</summary>
        public static void GenerateActionsFromCommandLine() => GenerateActions();

        static SerializedProperty RequireProperty(SerializedObject serialized, string name) {
            var property = serialized.FindProperty(name);
            if (property == null) throw new InvalidOperationException($"Could not find serialized GameInputConfig field '{name}'.");
            return property;
        }

        static void SetObject(SerializedObject serialized, string name, UnityEngine.Object value) =>
            RequireProperty(serialized, name).objectReferenceValue = value;

        static void SetString(SerializedObject serialized, string name, string value) =>
            RequireProperty(serialized, name).stringValue = value;

        static void SetBool(SerializedObject serialized, string name, bool value) =>
            RequireProperty(serialized, name).boolValue = value;

        static void SetEnum(SerializedObject serialized, string name, int value) =>
            RequireProperty(serialized, name).enumValueIndex = value;

        static void SetStringList(SerializedObject serialized, string name, IList<string> values) {
            var property = RequireProperty(serialized, name);
            property.ClearArray();
            foreach (string value in values.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.Ordinal)) {
                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).stringValue = value;
            }
        }

        static string NormalizeAssetPath(string path) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Asset path cannot be empty.", nameof(path));
            path = path.Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Split('/').Any(p => p == ".." || p == ".")) {
                throw new ArgumentException("Asset path must be project-relative and inside Assets.", nameof(path));
            }
            return path;
        }
    }

}
