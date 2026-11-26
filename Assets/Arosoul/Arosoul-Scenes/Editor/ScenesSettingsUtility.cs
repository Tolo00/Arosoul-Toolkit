using System;
using System.Collections.Generic;
using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Shared operations on Scenes settings.
    /// </summary>
    public static class ScenesSettingsUtility
    {
        /// <summary>
        /// Raised when the settings were changed by something other than the Scene Config window, so open windows can refresh.
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// Save the settings and bake the runtime catalog.
        /// </summary>
        public static ValidationReport Commit()
        {
            var settings = ScenesSettings.instance;
            settings.SaveSettings();
            ValidationReport report = SceneCatalogBaker.Bake();
            PlayModeBootstrap.UpdateRedirect();
            return report;
        }

        /// <summary>
        /// Raise <see cref="Changed"/> after the settings were changed outside of the Scene Config window.
        /// </summary>
        internal static void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Add a scene to the registered scenes.
        /// </summary>
        public static void RegisterScene(string guid)
        {
            RegisterScenes(new[] { guid });
        }

        /// <summary>
        /// Add scenes to the registered scenes. Scenes that are already registered are skipped. Saves and bakes once.
        /// </summary>
        /// <returns>The number of scenes that were added.</returns>
        public static int RegisterScenes(IEnumerable<string> guids)
        {
            var settings = ScenesSettings.instance;
            var toAdd = new List<string>();
            foreach (string guid in guids)
            {
                if (string.IsNullOrEmpty(guid) || toAdd.Contains(guid) || settings.Data.IsRegistered(guid)) continue;

                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path == PlayModeBootstrap.BootstrapScenePath) continue;
                toAdd.Add(guid);
            }
            if (toAdd.Count == 0) return 0;

            var so = new SerializedObject(settings);
            SerializedProperty scenes = so.FindProperty("_data._scenes");
            foreach (string guid in toAdd)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scenes.arraySize++;

                SerializedProperty element = scenes.GetArrayElementAtIndex(scenes.arraySize - 1);
                element.FindPropertyRelative("_scene._guid").stringValue = guid;
                element.FindPropertyRelative("_scene._path").stringValue = path;
                element.FindPropertyRelative("_scene._name").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                element.FindPropertyRelative("_dependencies").ClearArray();
            }
            ScenesUndo.Apply(so, toAdd.Count == 1 ? "Register Scene" : $"Register {toAdd.Count} Scenes");

            Commit();
            Changed?.Invoke();
            return toAdd.Count;
        }

        /// <summary>
        /// Register every scene in a folder and its subfolders.
        /// </summary>
        /// <param name="assetFolder">Project relative folder, for example Assets/Scenes.</param>
        public static int RegisterScenesInFolder(string assetFolder)
        {
            return RegisterScenes(AssetDatabase.FindAssets("t:Scene", new[] { assetFolder }));
        }
    }
}
