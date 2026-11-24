using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Implement on a MonoBehaviour in a scene to run asynchronous setup before the scene is considered ready.
    /// The operation waits for all initializers of a scene before it loads the next scene or plays the exit of its transition.
    /// Use it for building a level from save data, waiting for a network handshake, warming up systems and so on.
    /// </summary>
    public interface ISceneInitializer
    {
        /// <summary>
        /// Initializers with a lower order finish before initializers with a higher order start.
        /// Initializers with the same order run in parallel.
        /// </summary>
        int InitializationOrder => 0;

        Awaitable InitializeAsync(SceneContext context);
    }
}
