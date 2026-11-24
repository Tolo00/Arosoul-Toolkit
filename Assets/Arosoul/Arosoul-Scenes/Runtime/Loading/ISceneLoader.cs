using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Abstraction over how scenes are loaded. The default implementation uses Unity's SceneManager.
    /// Replace it to load scenes from another source, or to test Scenes without loading real scenes.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Load a scene additively. The scene must be fully loaded and activated when the awaitable completes.
        /// </summary>
        Awaitable<Scene> LoadAdditiveAsync(string path, Action<float> onProgress);

        Awaitable UnloadAsync(Scene scene, string path);

        void SetActive(Scene scene);

        Awaitable UnloadUnusedAssetsAsync();

        /// <summary>
        /// All initializers in a loaded scene.
        /// </summary>
        List<ISceneInitializer> GetInitializers(Scene scene, string path);
    }
}
