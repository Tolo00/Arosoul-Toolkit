using System.Collections.Generic;
using System.IO;
using Arosoul.Transitions.Editor;
using UnityEditor;
using UnityEngine;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Bakes the project settings into the runtime catalog, generates the id constants and syncs the build settings.
    /// </summary>
    public static class SceneCatalogBaker
    {
        public const string GeneratedFolder = "Assets/Generated/Scenes";
        public const string CatalogPath = GeneratedFolder + "/SceneCatalog.asset";
        public const string IdsPath = GeneratedFolder + "/SceneIds.cs";

        private static bool _pendingBake;

        /// <summary>
        /// Bake the catalog. Returns the validation report of the settings.
        /// While in play mode only the validation is done, the bake happens when play mode ends.
        /// </summary>
        public static ValidationReport Bake()
        {
            var settings = ScenesSettings.instance;
            settings.EnsureDefaults();

            ScenesData data = settings.Data.Clone();
            data.RefreshCaches(Resolve);

            ValidationReport report = ScenesValidator.Validate(data, new EditorSceneProjectContext(), settings.SyncBuildSettings);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _pendingBake = true;
                return report;
            }
            _pendingBake = false;

            List<ScenePathEntry> paths = CollectPaths(data);
            WriteCatalog(data, paths);

            if (settings.SyncBuildSettings)
                SceneBuildSettingsSync.Sync(paths.ConvertAll(p => p.Path));

            return report;
        }

        public static void BakePendingIfNeeded()
        {
            if (_pendingBake) Bake();
        }

        private static (string path, string name) Resolve(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? (null, null) : (path, Path.GetFileNameWithoutExtension(path));
        }

        private static List<ScenePathEntry> CollectPaths(ScenesData data)
        {
            var guids = new List<string>();
            void Add(SceneRef scene)
            {
                if (scene.IsValid && !guids.Contains(scene.Guid)) guids.Add(scene.Guid);
            }

            if (data.LoadCoreScene) Add(data.CoreScene);
            foreach (var definition in data.Scenes)
            {
                Add(definition.Scene);
                foreach (var dependency in definition.Dependencies) Add(dependency);
            }

            var result = new List<ScenePathEntry>();
            foreach (string guid in guids)
            {
                var (path, name) = Resolve(guid);
                if (string.IsNullOrEmpty(path)) continue;
                result.Add(new ScenePathEntry { Guid = guid, Path = path, Name = name });
            }
            return result;
        }

        private static void WriteCatalog(ScenesData data, List<ScenePathEntry> paths)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(GeneratedFolder);
                catalog = ScriptableObject.CreateInstance<SceneCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            if (catalog.Initialize(data, paths))
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
            GeneratedFileUtility.EnsurePreloaded(catalog);
        }

        /// <summary>
        /// The source of the generated SceneIds file for the given data.
        /// </summary>
        public static string BuildIdsSource(ScenesData data)
        {
            var slots = new IdClassSpec("SlotIds", nameof(SlotId), "Type-safe ids of all scene slots.");
            AddEntries(slots, Collect(data.Slots, s => s.Key, s => $"new {nameof(SlotId)}({s.Id.Value})"));

            var scenes = new IdClassSpec("SceneIds", nameof(SceneRef), "References to all registered scenes.");
            AddEntries(scenes, Collect(data.Scenes, s => s.Scene.Name, s => $"{nameof(SceneRef)}.{nameof(SceneRef.FromGuid)}(\"{s.Scene.Guid}\")"));

            // Generated ids live in the global namespace, so game code can use them without a using directive.
            return IdCodeGenerator.Generate(
                null,
                new[] { "Arosoul.Scenes" },
                new[] { slots, scenes },
                "Arosoul Scenes");
        }

        /// <summary>
        /// True if the generated file is missing or differs from what the current settings would produce.
        /// </summary>
        public static bool IsIdsOutOfSync()
        {
            string expected = BuildIdsSource(GetRefreshedData());
            return !File.Exists(IdsPath) || File.ReadAllText(IdsPath) != expected;
        }

        /// <summary>
        /// Write the generated file. Returns true if it was written. Not possible in play mode.
        /// </summary>
        public static bool GenerateIds()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            return GeneratedFileUtility.WriteIfChanged(IdsPath, BuildIdsSource(GetRefreshedData()));
        }

        // Scene names come from the asset database, so refresh them the same way Bake does.
        private static ScenesData GetRefreshedData()
        {
            ScenesData data = ScenesSettings.instance.Data.Clone();
            data.RefreshCaches(Resolve);
            return data;
        }

        private static List<(string name, string expression)> Collect<T>(IReadOnlyList<T> items, System.Func<T, string> name, System.Func<T, string> expression)
        {
            var result = new List<(string, string)>();
            foreach (T item in items)
            {
                string itemName = name(item);
                if (string.IsNullOrWhiteSpace(itemName)) continue;
                result.Add((itemName, expression(item)));
            }
            return result;
        }

        private static void AddEntries(IdClassSpec spec, List<(string name, string expression)> entries)
        {
            var names = IdentifierUtility.MakeUnique(entries.ConvertAll(e => e.name));
            for (int i = 0; i < entries.Count; i++)
                spec.Entries.Add(new IdEntry(names[i], entries[i].expression));
        }

        [MenuItem("Tools/Arosoul/Scenes/Rebuild Catalog", priority = 108)]
        private static void RebuildMenu()
        {
            ScenesSettingsUtility.Commit();
        }
    }
}
