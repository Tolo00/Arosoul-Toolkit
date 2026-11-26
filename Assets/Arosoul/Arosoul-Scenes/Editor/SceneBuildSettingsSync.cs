using System.Collections.Generic;
using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Makes sure registered scenes are in the build settings. Existing entries and their order are kept,
    /// except entries of scenes that no longer exist, which are removed.
    /// </summary>
    public static class SceneBuildSettingsSync
    {
        /// <returns>The number of changes made.</returns>
        public static int Sync(IEnumerable<string> scenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Deleted scenes would otherwise stay in the list forever.
            int changes = scenes.RemoveAll(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null);

            foreach (string path in scenePaths)
            {
                if (string.IsNullOrEmpty(path)) continue;

                int index = scenes.FindIndex(s => s.path == path);
                if (index < 0)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    changes++;
                }
                else if (!scenes[index].enabled)
                {
                    scenes[index] = new EditorBuildSettingsScene(path, true);
                    changes++;
                }
            }

            if (changes > 0)
                EditorBuildSettings.scenes = scenes.ToArray();
            return changes;
        }
    }
}
