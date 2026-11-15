using System;
using System.IO;
using UnityEngine;

namespace ProjectWizard
{
    internal static class WizardPaths
    {
        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string ManifestPath => Path.Combine(ProjectRoot, "Packages", "manifest.json");

        /// <summary>Where NuGetForUnity keeps its package list.</summary>
        public static string PackagesConfigPath => Path.Combine(Application.dataPath, "packages.config");

        public static string ToFullPath(string projectRelative) =>
            Path.GetFullPath(Path.Combine(ProjectRoot, projectRelative));

        /// <summary>Folder that holds downloaded Asset Store .unitypackage files.</summary>
        public static string AssetStoreCache
        {
            get
            {
                if (!string.IsNullOrEmpty(WizardConfig.AssetStoreCacheOverride))
                    return WizardConfig.AssetStoreCacheOverride;

                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Unity", "Asset Store-5.x");

                    case RuntimePlatform.OSXEditor:
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                            "Library", "Unity", "Asset Store-5.x");

                    default: // Linux
                        return Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                            ".local", "share", "unity3d", "Asset Store-5.x");
                }
            }
        }
    }
}
