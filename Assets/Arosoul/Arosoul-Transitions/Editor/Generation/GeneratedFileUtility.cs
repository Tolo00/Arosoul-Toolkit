using System.IO;
using UnityEditor;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Helpers for writing generated files without causing needless recompiles.
    /// </summary>
    public static class GeneratedFileUtility
    {
        /// <summary>
        /// Writes the file only if the content differs. Returns true if the file was written.
        /// </summary>
        public static bool WriteIfChanged(string assetPath, string content)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            if (File.Exists(assetPath) && File.ReadAllText(assetPath) == content)
                return false;

            File.WriteAllText(assetPath, content);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        /// <summary>
        /// Adds an asset to the preloaded assets of the player settings, if it is not already there.
        /// </summary>
        public static void EnsurePreloaded(UnityEngine.Object asset)
        {
            if (asset == null) return;

            var preloaded = new System.Collections.Generic.List<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());
            if (preloaded.Contains(asset)) return;

            preloaded.RemoveAll(a => a == null);
            preloaded.Add(asset);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }
    }
}
