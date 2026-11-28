using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Bakes the project settings into the runtime settings asset and keeps it in the preloaded assets.
    /// </summary>
    public static class CommandsSettingsBaker
    {
        public const string GeneratedFolder = "Assets/Generated/Commands";
        public const string RuntimeSettingsPath = GeneratedFolder + "/CommandsRuntimeSettings.asset";

        private const string DefaultThemeName = "UnityDefaultRuntimeTheme";

        /// <summary>
        /// Save the settings, then write them to the runtime settings asset if they changed.
        /// </summary>
        public static CommandsRuntimeSettings Bake()
        {
            CommandsSettings settings = CommandsSettings.instance;
            if (settings.Data.Theme == null)
            {
                settings.Data.Theme = FindDefaultTheme();
                if (settings.Data.Theme != null) settings.SaveSettings();
            }

            CommandsRuntimeSettings runtime = AssetDatabase.LoadAssetAtPath<CommandsRuntimeSettings>(RuntimeSettingsPath);
            if (runtime == null)
            {
                EnsureFolder(GeneratedFolder);
                runtime = ScriptableObject.CreateInstance<CommandsRuntimeSettings>();
                runtime.SetData(settings.Data.Clone());
                AssetDatabase.CreateAsset(runtime, RuntimeSettingsPath);
            }
            else if (!IsInSync(settings.Data, runtime.Data))
            {
                runtime.SetData(settings.Data.Clone());
                EditorUtility.SetDirty(runtime);
                AssetDatabase.SaveAssetIfDirty(runtime);
            }

            EnsurePreloaded(runtime);
            return runtime;
        }

        public static bool IsInSync(CommandsSettingsData a, CommandsSettingsData b)
        {
            return EditorJsonUtility.ToJson(new DataWrapper(a)) == EditorJsonUtility.ToJson(new DataWrapper(b));
        }

        public static bool IsPreloaded(Object asset)
        {
            return asset != null && PlayerSettings.GetPreloadedAssets().Contains(asset);
        }

        private static void EnsurePreloaded(Object asset)
        {
            if (IsPreloaded(asset)) return;

            var preloaded = new List<Object>(PlayerSettings.GetPreloadedAssets()) { asset };
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static ThemeStyleSheet FindDefaultTheme()
        {
            ThemeStyleSheet fallback = null;
            foreach (string guid in AssetDatabase.FindAssets("t:ThemeStyleSheet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                if (theme == null) continue;
                if (Path.GetFileNameWithoutExtension(path) == DefaultThemeName) return theme;

                fallback ??= theme;
            }
            return fallback;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// EditorJsonUtility serializes object references by asset, which plain JsonUtility does not.
        /// </summary>
        [System.Serializable]
        private class DataWrapper
        {
            public CommandsSettingsData Data;

            public DataWrapper(CommandsSettingsData data)
            {
                Data = data;
            }
        }
    }

    /// <summary>
    /// Bakes the settings before every build and reports whether the console is included.
    /// </summary>
    internal class CommandsBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -80;

        public void OnPreprocessBuild(BuildReport report)
        {
            CommandsRuntimeSettings runtime = CommandsSettingsBaker.Bake();
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            bool included = runtime.Data.Availability == CommandsAvailability.AllBuilds ||
                            (runtime.Data.Availability == CommandsAvailability.DevelopmentBuilds && development);

            if (!included)
                Debug.Log($"Arosoul Commands: the console is disabled in this build (availability {runtime.Data.Availability}, development build {development}).");
        }
    }

    /// <summary>
    /// Creates the runtime settings asset the first time the package is used in a project.
    /// </summary>
    [InitializeOnLoad]
    internal static class CommandsSettingsBootstrap
    {
        static CommandsSettingsBootstrap()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (AssetDatabase.LoadAssetAtPath<CommandsRuntimeSettings>(CommandsSettingsBaker.RuntimeSettingsPath) == null)
                    CommandsSettingsBaker.Bake();
            };
        }
    }
}
