using UnityEngine.SceneManagement;

namespace Arosoul.Scenes
{
    /// <summary>
    /// A scene that is loaded by Scenes.
    /// </summary>
    public sealed class LoadedSceneInfo
    {
        public string Guid { get; }
        public string Name { get; }
        public string Path { get; }
        public Scene Scene { get; }

        /// <summary>
        /// The slot the scene occupies. None if the scene is only loaded because something depends on it.
        /// </summary>
        public SlotId Slot { get; internal set; }

        public bool IsCore { get; internal set; }

        /// <summary>
        /// Seconds it took to load the scene, excluding initialization.
        /// </summary>
        public float LoadDuration { get; internal set; }

        /// <summary>
        /// Seconds it took to run all scene initializers.
        /// </summary>
        public float InitializationDuration { get; internal set; }

        public bool IsDependencyOnly => Slot.IsNone && !IsCore;

        public LoadedSceneInfo(string guid, string name, string path, Scene scene)
        {
            Guid = guid;
            Name = name;
            Path = path;
            Scene = scene;
        }
    }
}
