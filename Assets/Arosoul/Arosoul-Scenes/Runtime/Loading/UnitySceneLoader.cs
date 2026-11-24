using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Arosoul.Scenes
{
    /// <summary>
    /// Loads scenes through Unity's SceneManager.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public async Awaitable<Scene> LoadAdditiveAsync(string path, Action<float> onProgress)
        {
            AsyncOperation operation = StartLoad(path);
            if (operation == null)
                throw new InvalidOperationException($"Unable to load scene '{path}'. Make sure it exists and is included in the build settings.");

            while (!operation.isDone)
            {
                onProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                await Awaitable.NextFrameAsync();
            }
            onProgress?.Invoke(1f);

            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Scene '{path}' was loaded but could not be found.");
            return scene;
        }

        private static AsyncOperation StartLoad(string path)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive);

#if UNITY_EDITOR
            // In the editor, scenes that are not in the build settings can still be loaded in play mode.
            if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
                return EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
#endif
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(path, parameters);
        }

        public async Awaitable UnloadAsync(Scene scene, string path)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
                await Awaitable.NextFrameAsync();
        }

        public void SetActive(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        }

        public async Awaitable UnloadUnusedAssetsAsync()
        {
            AsyncOperation operation = Resources.UnloadUnusedAssets();
            while (!operation.isDone)
                await Awaitable.NextFrameAsync();
        }

        public List<ISceneInitializer> GetInitializers(Scene scene, string path)
        {
            var result = new List<ISceneInitializer>();
            if (!scene.IsValid() || !scene.isLoaded) return result;

            var buffer = new List<MonoBehaviour>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                root.GetComponentsInChildren(true, buffer);
                for (int i = 0; i < buffer.Count; i++)
                    if (buffer[i] is ISceneInitializer initializer) result.Add(initializer);
            }
            return result;
        }
    }
}
