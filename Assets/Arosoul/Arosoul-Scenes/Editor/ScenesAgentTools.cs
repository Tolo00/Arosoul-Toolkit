using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arosoul.Transitions.Editor;
using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Public editor API for configuring Scenes without driving the Scene Config window.
    /// Call it from an editor script, a batch mode -executeMethod entry point or an automation bridge.
    ///
    /// Scenes are identified by their project relative path (Assets/Scenes/Level.unity), slots by their key.
    /// Every method that changes the settings saves them, bakes the runtime catalog and regenerates SceneIds.cs before it returns,
    /// so the generated SlotIds and SceneIds are usable after the next compilation. Changes are recorded as undo steps.
    /// </summary>
    public static class ScenesAgentTools
    {
        private static ScenesSettings Settings
        {
            get
            {
                ScenesSettings settings = ScenesSettings.instance;
                settings.EnsureDefaults();
                return settings;
            }
        }

        /// <summary>
        /// The current settings. Read only, use the methods of this class to change them.
        /// </summary>
        public static ScenesData Data => Settings.Data;

        #region Scenes

        /// <summary>
        /// Register a scene, and optionally replace its dependencies. Registering an already registered scene only updates its dependencies.
        /// </summary>
        /// <param name="scenePath">Project relative path of the scene, for example Assets/Scenes/Level.unity.</param>
        /// <param name="dependencyPaths">Scenes that must be loaded before this scene. Pass null to keep the current dependencies.</param>
        public static SceneRef RegisterScene(string scenePath, IEnumerable<string> dependencyPaths = null)
        {
            string guid = ResolveSceneGuid(scenePath, nameof(scenePath));
            ScenesSettingsUtility.RegisterScenes(new[] { guid });

            if (dependencyPaths != null)
                SetDependencies(scenePath, dependencyPaths);
            else
                CommitAndGenerateIds();

            return MakeRef(guid);
        }

        /// <summary>
        /// Register every scene in a folder and its subfolders.
        /// </summary>
        /// <returns>The number of scenes that were added.</returns>
        public static int RegisterScenesInFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder) || !AssetDatabase.IsValidFolder(assetFolder.TrimEnd('/')))
                throw new ArgumentException($"'{assetFolder}' is not a folder in the project.", nameof(assetFolder));

            int added = ScenesSettingsUtility.RegisterScenesInFolder(assetFolder.TrimEnd('/'));
            CommitAndGenerateIds();
            return added;
        }

        /// <summary>
        /// Remove a scene from the registered scenes. References to it as a dependency or core scene are kept.
        /// </summary>
        /// <returns>True if the scene was registered.</returns>
        public static bool UnregisterScene(string scenePath)
        {
            string guid = ResolveSceneGuid(scenePath, nameof(scenePath));
            return Modify("Unregister Scene", so =>
            {
                int index = FindSceneIndex(guid);
                if (index < 0) return false;

                so.FindProperty("_data._scenes").DeleteArrayElementAtIndex(index);
                return true;
            });
        }

        /// <summary>
        /// Replace the dependencies of a registered scene. Dependencies are loaded, in order, before the scene.
        /// </summary>
        public static void SetDependencies(string scenePath, IEnumerable<string> dependencyPaths)
        {
            string guid = ResolveSceneGuid(scenePath, nameof(scenePath));
            if (dependencyPaths == null) throw new ArgumentNullException(nameof(dependencyPaths));

            List<string> dependencies = dependencyPaths.Select(p => ResolveSceneGuid(p, nameof(dependencyPaths))).Distinct().ToList();
            if (dependencies.Contains(guid))
                throw new ArgumentException("A scene can not depend on itself.", nameof(dependencyPaths));

            int index = FindSceneIndex(guid);
            if (index < 0)
                throw new InvalidOperationException($"'{scenePath}' is not registered. Call RegisterScene first.");

            Modify("Set Scene Dependencies", so =>
            {
                SerializedProperty list = so.FindProperty("_data._scenes").GetArrayElementAtIndex(index).FindPropertyRelative("_dependencies");
                list.ClearArray();
                foreach (string dependency in dependencies)
                {
                    list.arraySize++;
                    WriteSceneRef(list.GetArrayElementAtIndex(list.arraySize - 1), dependency);
                }
                return true;
            });
        }

        /// <summary>
        /// Set the core scene, a dependency of every other scene that is never unloaded. Pass null to disable the core scene.
        /// </summary>
        public static void SetCoreScene(string scenePath)
        {
            string guid = string.IsNullOrEmpty(scenePath) ? null : ResolveSceneGuid(scenePath, nameof(scenePath));
            Modify("Set Core Scene", so =>
            {
                so.FindProperty("_data._loadCoreScene").boolValue = guid != null;
                WriteSceneRef(so.FindProperty("_data._coreScene"), guid ?? "");
                return true;
            });
        }

        #endregion

        #region Slots

        /// <summary>
        /// Add a slot. The key becomes the generated SlotIds member name.
        /// </summary>
        /// <returns>The id of the new slot.</returns>
        public static SlotId AddSlot(string key)
        {
            key = ValidateSlotKey(key);
            if (FindSlotIndex(key) >= 0)
                throw new ArgumentException($"A slot with the key '{key}' already exists.", nameof(key));

            int id = 0;
            Modify("Add Slot", so =>
            {
                SerializedProperty next = so.FindProperty("_data._nextSlotId");
                SerializedProperty slots = so.FindProperty("_data._slots");
                id = next.intValue++;

                slots.arraySize++;
                SerializedProperty element = slots.GetArrayElementAtIndex(slots.arraySize - 1);
                element.FindPropertyRelative("_id").intValue = id;
                element.FindPropertyRelative("_key").stringValue = key;
                return true;
            });
            return new SlotId(id);
        }

        /// <summary>
        /// Change the key of a slot. Its stored id is kept, so serialized SlotId fields keep working.
        /// </summary>
        public static void RenameSlot(string key, string newKey)
        {
            int index = RequireSlotIndex(key);
            newKey = ValidateSlotKey(newKey);
            if (newKey != key && FindSlotIndex(newKey) >= 0)
                throw new ArgumentException($"A slot with the key '{newKey}' already exists.", nameof(newKey));

            Modify("Rename Slot", so =>
            {
                so.FindProperty("_data._slots").GetArrayElementAtIndex(index).FindPropertyRelative("_key").stringValue = newKey;
                return true;
            });
        }

        /// <summary>
        /// Remove a slot. Its id is not reused.
        /// </summary>
        /// <returns>True if the slot existed.</returns>
        public static bool RemoveSlot(string key)
        {
            int index = FindSlotIndex(key);
            if (index < 0) return false;

            return Modify("Remove Slot", so =>
            {
                so.FindProperty("_data._slots").DeleteArrayElementAtIndex(index);
                return true;
            });
        }

        /// <summary>
        /// Set the slot the first scene occupies when the game starts.
        /// </summary>
        public static void SetInitialSlot(string key)
        {
            int index = RequireSlotIndex(key);
            int id = Data.Slots[index].Id.Value;
            Modify("Set Initial Slot", so =>
            {
                so.FindProperty("_data._initialSlot._value").intValue = id;
                return true;
            });
        }

        /// <summary>
        /// Find a slot id by its key. Returns <see cref="SlotId.None"/> if no slot uses the key.
        /// </summary>
        public static SlotId GetSlot(string key)
        {
            int index = FindSlotIndex(key);
            return index < 0 ? SlotId.None : Data.Slots[index].Id;
        }

        #endregion

        #region Options and validation

        /// <summary>
        /// Change the editor options. Pass null to keep a value.
        /// </summary>
        /// <param name="playModeBootstrap">Start play mode through a bootstrap scene so dependencies of the open scene load first.</param>
        /// <param name="syncBuildSettings">Keep the build settings scene list in sync with the referenced scenes.</param>
        public static void SetOptions(bool? playModeBootstrap = null, bool? syncBuildSettings = null)
        {
            Modify("Change Scenes Options", so =>
            {
                if (playModeBootstrap.HasValue) so.FindProperty("_playModeBootstrap").boolValue = playModeBootstrap.Value;
                if (syncBuildSettings.HasValue) so.FindProperty("_syncBuildSettings").boolValue = syncBuildSettings.Value;
                return true;
            });
        }

        /// <summary>
        /// Save the settings, bake the catalog and regenerate SceneIds.cs. Call it after changing the settings in another way,
        /// for example after renaming or moving scenes.
        /// </summary>
        /// <returns>The diagnostics of the configuration, see <see cref="Validate"/>.</returns>
        public static string[] Commit()
        {
            return ToDiagnostics(CommitAndGenerateIds());
        }

        /// <summary>
        /// Diagnostics of the configuration. Entries prefixed with ERROR must be fixed (they fail builds), WARNING entries are advisory.
        /// An empty array means the configuration is valid and the generated files are up to date.
        /// </summary>
        public static string[] Validate()
        {
            ScenesSettings settings = Settings;
            ScenesData data = settings.Data.Clone();
            data.RefreshCaches(ResolvePathAndName);

            var diagnostics = new List<string>(ToDiagnostics(ScenesValidator.Validate(data, new EditorSceneProjectContext(), settings.SyncBuildSettings)));
            if (SceneCatalogBaker.IsIdsOutOfSync())
                diagnostics.Add($"WARNING: '{SceneCatalogBaker.IdsPath}' is out of sync with the configuration. Call ScenesAgentTools.Commit().");
            if (AssetDatabase.LoadAssetAtPath<SceneCatalog>(SceneCatalogBaker.CatalogPath) == null)
                diagnostics.Add($"ERROR: No runtime catalog at '{SceneCatalogBaker.CatalogPath}'. Call ScenesAgentTools.Commit().");
            return diagnostics.ToArray();
        }

        /// <summary>
        /// Readable summary of the configuration: options, core scene, slots and registered scenes with their dependencies.
        /// </summary>
        public static string Describe()
        {
            ScenesSettings settings = Settings;
            ScenesData data = settings.Data.Clone();
            data.RefreshCaches(ResolvePathAndName);

            var sb = new StringBuilder();
            sb.Append($"PlayModeBootstrap={settings.PlayModeBootstrap} SyncBuildSettings={settings.SyncBuildSettings}\n");
            sb.Append("Core scene: ").Append(data.LoadCoreScene ? Format(data.CoreScene) : "(disabled)").Append('\n');

            string initialKey = data.Slots.FirstOrDefault(s => s.Id == data.InitialSlot)?.Key ?? "(none)";
            sb.Append("Initial slot: ").Append(initialKey).Append('\n');

            List<string> slotNames = IdentifierUtility.MakeUnique(data.Slots.Select(s => s.Key).ToList());
            sb.Append("Slots (").Append(data.Slots.Count).Append("):\n");
            for (int i = 0; i < data.Slots.Count; i++)
                sb.Append($"- SlotIds.{slotNames[i]} id={data.Slots[i].Id.Value}\n");

            List<string> sceneNames = IdentifierUtility.MakeUnique(data.Scenes.Select(s => s.Scene.Name).ToList());
            sb.Append("Scenes (").Append(data.Scenes.Count).Append("):\n");
            for (int i = 0; i < data.Scenes.Count; i++)
            {
                SceneDefinition scene = data.Scenes[i];
                sb.Append($"- SceneIds.{sceneNames[i]} ").Append(Format(scene.Scene));
                if (scene.Dependencies.Count > 0)
                    sb.Append(" depends on ").Append(string.Join(", ", scene.Dependencies.Select(Format)));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        #endregion

        #region Helpers

        // Applies a change to the settings as an undo step, then saves, bakes and regenerates the ids.
        private static bool Modify(string undoName, Func<SerializedObject, bool> change)
        {
            var so = new SerializedObject(Settings);
            so.Update();
            if (!change(so)) return false;

            ScenesUndo.Apply(so, undoName);
            CommitAndGenerateIds();
            return true;
        }

        private static ValidationReport CommitAndGenerateIds()
        {
            ValidationReport report = ScenesSettingsUtility.Commit();
            SceneCatalogBaker.GenerateIds();
            ScenesSettingsUtility.NotifyChanged();
            return report;
        }

        private static string[] ToDiagnostics(ValidationReport report)
        {
            return report.Issues
                .Where(i => i.Severity != ValidationSeverity.Info)
                .Select(i => $"{(i.Severity == ValidationSeverity.Error ? "ERROR" : "WARNING")}: {i.Message}")
                .ToArray();
        }

        private static string ResolveSceneGuid(string scenePath, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                throw new ArgumentException("Scene path can not be empty.", parameterName);

            scenePath = scenePath.Replace('\\', '/').Trim();
            if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"'{scenePath}' is not a scene path. Use a project relative path ending in .unity.", parameterName);

            string guid = AssetDatabase.AssetPathToGUID(scenePath);
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                throw new ArgumentException($"No scene found at '{scenePath}'.", parameterName);
            if (scenePath == PlayModeBootstrap.BootstrapScenePath)
                throw new ArgumentException("The generated play mode bootstrap scene can not be configured.", parameterName);
            return guid;
        }

        private static (string path, string name) ResolvePathAndName(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? (null, null) : (path, Path.GetFileNameWithoutExtension(path));
        }

        private static SceneRef MakeRef(string guid)
        {
            var (path, name) = ResolvePathAndName(guid);
            return new SceneRef(guid, path ?? "", name ?? "");
        }

        private static void WriteSceneRef(SerializedProperty property, string guid)
        {
            var (path, name) = string.IsNullOrEmpty(guid) ? ("", "") : ResolvePathAndName(guid);
            property.FindPropertyRelative("_guid").stringValue = guid;
            property.FindPropertyRelative("_path").stringValue = path ?? "";
            property.FindPropertyRelative("_name").stringValue = name ?? "";
        }

        private static int FindSceneIndex(string guid)
        {
            IReadOnlyList<SceneDefinition> scenes = Data.Scenes;
            for (int i = 0; i < scenes.Count; i++)
                if (scenes[i].Scene.Guid == guid) return i;
            return -1;
        }

        private static int FindSlotIndex(string key)
        {
            IReadOnlyList<SlotDefinition> slots = Data.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].Key == key) return i;
            return -1;
        }

        private static int RequireSlotIndex(string key)
        {
            int index = FindSlotIndex(key);
            if (index < 0)
                throw new ArgumentException($"No slot with the key '{key}'. Existing slots: {string.Join(", ", Data.Slots.Select(s => s.Key))}.", nameof(key));
            return index;
        }

        private static string ValidateSlotKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Slot key can not be empty.", nameof(key));
            return key.Trim();
        }

        private static string Format(SceneRef scene)
        {
            return scene.IsValid ? (string.IsNullOrEmpty(scene.Path) ? $"(missing scene {scene.Guid})" : scene.Path) : "(none)";
        }

        #endregion
    }
}
