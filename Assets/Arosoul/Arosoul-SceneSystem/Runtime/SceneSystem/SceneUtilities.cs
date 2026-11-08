using UnityEngine;

namespace Arosoul.SceneSystem
{
    public static class SceneUtilities 
    {
        /// <summary>
        /// Return build index by scene name.
        /// Requires that the scene is added to build setting.
        /// </summary>
        public static int GetBuildIndexByName(string sceneName) {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string loadedSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (loadedSceneName == sceneName) {
                    return i;
                }
            }

            Debug.LogError($"Scene ({sceneName}) could not be found in build settings.");
            return -1;
        }

        /// <summary>
        /// Check if a scene is loaded inside of slot.
        /// </summary>
        public static bool IsSlotLoaded(string slotKey)
        {
            return SceneManager.IsSlotLoaded(slotKey);
        }

        /// <summary>
        /// Check if scene is loaded.
        /// </summary>
        /// <param name="sceneName">Name of scene to check.</param>
        /// <param name="slotKey">The slot which it is loaded in.</param>
        public static bool IsSceneLoaded(string sceneName, out string slotKey)
        {
            return SceneManager.IsSceneLoaded(sceneName, out slotKey);
        }
    }
}
