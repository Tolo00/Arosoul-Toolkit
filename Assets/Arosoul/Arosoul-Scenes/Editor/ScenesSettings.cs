using UnityEditor;
using UnityEngine;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Project wide Scenes settings, stored in ProjectSettings/ScenesSettings.asset and edited in the Scene Config window
    /// (Tools > Scene System > Scene Config). The runtime reads a baked copy, see <see cref="SceneCatalog"/>.
    /// </summary>
    [FilePath("ProjectSettings/ScenesSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class ScenesSettings : ScriptableSingleton<ScenesSettings>
    {
        [SerializeField] private ScenesData _data = new ScenesData();
        [SerializeField] private bool _defaultsCreated;

        [SerializeField, Tooltip("When pressing play in a scene that has dependencies, start through a bootstrap scene so dependencies are loaded first.")]
        private bool _playModeBootstrap = true;

        [SerializeField, Tooltip("Keep the build settings scene list in sync with the registered scenes.")]
        private bool _syncBuildSettings = true;

        public ScenesData Data => _data;
        public bool PlayModeBootstrap => _playModeBootstrap;
        public bool SyncBuildSettings => _syncBuildSettings;

        /// <summary>
        /// Creates a default slot the first time the settings are used.
        /// </summary>
        public void EnsureDefaults()
        {
            if (_defaultsCreated) return;
            _defaultsCreated = true;

            if (_data.Slots.Count == 0)
            {
                SlotDefinition main = _data.AddSlot("Main");
                _data.SetInitialSlot(main.Id);
            }
            Save(true);
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }
}
