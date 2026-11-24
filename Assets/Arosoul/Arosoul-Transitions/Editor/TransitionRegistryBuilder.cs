using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Keeps the transition registry, the stable ids and the generated id constants in sync with the transition definition assets.
    /// Runs automatically when a definition is created, changed, moved or deleted.
    /// </summary>
    public static class TransitionRegistryBuilder
    {
        public const string GeneratedFolder = "Assets/Generated/Transitions";
        public const string DefinitionsFolder = GeneratedFolder + "/Definitions";
        public const string RegistryPath = GeneratedFolder + "/TransitionRegistry.asset";
        public const string IdsPath = GeneratedFolder + "/TransitionIds.cs";

        private static bool _refreshing;
        private static bool _scheduled;

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            ScheduleRefresh();
        }

        /// <summary>
        /// Refresh on the next editor update. Multiple calls are merged into one.
        /// </summary>
        public static void ScheduleRefresh()
        {
            if (_scheduled) return;
            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                Refresh();
            };
        }

        public static List<TransitionDefinition> FindDefinitions()
        {
            var result = new List<TransitionDefinition>();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(TransitionDefinition)}");
            Array.Sort(guids, (a, b) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            foreach (string guid in guids)
            {
                var definition = AssetDatabase.LoadAssetAtPath<TransitionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null) result.Add(definition);
            }
            return result;
        }

        [MenuItem("Tools/Transition System/Regenerate Ids")]
        public static void RefreshMenu()
        {
            Refresh();
        }

        public static void Refresh()
        {
            if (_refreshing) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleRefresh();
                return;
            }

            _refreshing = true;
            try
            {
                EnsureDefaultDefinitions();

                var definitions = FindDefinitions();
                AssignKeysAndIds(definitions);
                WriteRegistry(definitions);
                WriteIds(definitions);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private static void AssignKeysAndIds(List<TransitionDefinition> definitions)
        {
            var currentIds = definitions.Select(d => d.Id.Value).ToList();
            int[] ids = StableIdAllocator.Allocate(currentIds);

            bool changed = false;
            for (int i = 0; i < definitions.Count; i++)
            {
                var so = new SerializedObject(definitions[i]);
                var idProp = so.FindProperty("_id");
                var keyProp = so.FindProperty("_key");

                if (idProp.intValue != ids[i])
                {
                    idProp.intValue = ids[i];
                    changed = true;
                }
                if (string.IsNullOrEmpty(keyProp.stringValue))
                {
                    keyProp.stringValue = IdentifierUtility.Sanitize(definitions[i].name);
                    changed = true;
                }

                if (so.ApplyModifiedPropertiesWithoutUndo())
                    EditorUtility.SetDirty(definitions[i]);
            }

            if (changed) AssetDatabase.SaveAssets();
        }

        private static void WriteRegistry(List<TransitionDefinition> definitions)
        {
            var registry = AssetDatabase.LoadAssetAtPath<TransitionRegistry>(RegistryPath);
            if (registry == null)
            {
                Directory.CreateDirectory(GeneratedFolder);
                registry = ScriptableObject.CreateInstance<TransitionRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            bool same = registry.Definitions.Count == definitions.Count;
            for (int i = 0; same && i < definitions.Count; i++)
                same = registry.Definitions[i] == definitions[i];

            if (!same)
            {
                registry.SetDefinitions(definitions);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            GeneratedFileUtility.EnsurePreloaded(registry);
        }

        private static void WriteIds(List<TransitionDefinition> definitions)
        {
            var names = IdentifierUtility.MakeUnique(definitions.Select(d => d.Key).ToList());

            var spec = new IdClassSpec("TransitionIds", nameof(TransitionId), "Type-safe ids of all transitions in the project.");
            for (int i = 0; i < definitions.Count; i++)
                spec.Entries.Add(new IdEntry(names[i], $"new {nameof(TransitionId)}({definitions[i].Id.Value})"));

            string source = IdCodeGenerator.Generate(
                "Arosoul.Generated",
                new[] { "Arosoul.Transitions" },
                new[] { spec },
                "Arosoul Transitions");

            GeneratedFileUtility.WriteIfChanged(IdsPath, source);
        }

        // Creates the built-in transitions the first time the package is used in a project.
        private static void EnsureDefaultDefinitions()
        {
            string key = "Arosoul.Transitions.DefaultsCreated." + Application.dataPath;
            if (EditorPrefs.GetBool(key, false)) return;
            EditorPrefs.SetBool(key, true);

            if (FindDefinitions().Count > 0) return;

            Directory.CreateDirectory(DefinitionsFolder);
            CreateDefinition<FadeTransition>("Fade");
            CreateDefinition<SlideTransition>("Slide");
            CreateDefinition<LoadingScreenTransition>("LoadingScreen");
            AssetDatabase.SaveAssets();
        }

        public static TransitionDefinition CreateDefinition(Type type, string name)
        {
            Directory.CreateDirectory(DefinitionsFolder);
            var definition = (TransitionDefinition)ScriptableObject.CreateInstance(type);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefinitionsFolder}/{name}.asset");
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static void CreateDefinition<T>(string name) where T : TransitionDefinition
        {
            CreateDefinition(typeof(T), name);
        }
    }

    internal class TransitionAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (HasDefinition(imported) || HasDefinition(moved) || deleted.Any(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
                TransitionRegistryBuilder.ScheduleRefresh();
        }

        private static bool HasDefinition(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type != null && typeof(TransitionDefinition).IsAssignableFrom(type)) return true;
            }
            return false;
        }
    }

    internal class TransitionBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            TransitionRegistryBuilder.Refresh();
        }
    }
}
