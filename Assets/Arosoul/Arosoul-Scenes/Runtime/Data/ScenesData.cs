using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Scenes
{
    [Serializable]
    public class SlotDefinition
    {
        [SerializeField] private int _id;
        [SerializeField] private string _key = "";

        public SlotDefinition() { }

        public SlotDefinition(int id, string key)
        {
            _id = id;
            _key = key;
        }

        public SlotId Id => new SlotId(_id);
        public string Key => _key ?? "";
    }

    /// <summary>
    /// A registered scene and the scenes that must be loaded before it.
    /// </summary>
    [Serializable]
    public class SceneDefinition
    {
        [SerializeField] private SceneRef _scene;
        [SerializeField] private List<SceneRef> _dependencies = new List<SceneRef>();

        public SceneDefinition() { }

        public SceneDefinition(SceneRef scene, params SceneRef[] dependencies)
        {
            _scene = scene;
            _dependencies = new List<SceneRef>(dependencies);
        }

        public SceneRef Scene => _scene;
        public IReadOnlyList<SceneRef> Dependencies => _dependencies;

        internal void RefreshCaches(Func<string, (string path, string name)> resolve)
        {
            _scene = Refresh(_scene, resolve);
            for (int i = 0; i < _dependencies.Count; i++)
                _dependencies[i] = Refresh(_dependencies[i], resolve);
        }

        internal static SceneRef Refresh(SceneRef sceneRef, Func<string, (string path, string name)> resolve)
        {
            if (!sceneRef.IsValid) return sceneRef;
            var (path, name) = resolve(sceneRef.Guid);
            return string.IsNullOrEmpty(path) ? sceneRef : sceneRef.WithCache(path, name);
        }
    }

    /// <summary>
    /// All user configured data of Scenes. Edited in the Scene Config window and baked into a runtime catalog.
    /// </summary>
    [Serializable]
    public class ScenesData
    {
        [SerializeField] private bool _loadCoreScene;
        [SerializeField] private SceneRef _coreScene;
        [SerializeField] private SlotId _initialSlot;
        [SerializeField] private int _nextSlotId = 1;
        [SerializeField] private List<SlotDefinition> _slots = new List<SlotDefinition>();
        [SerializeField] private List<SceneDefinition> _scenes = new List<SceneDefinition>();

        /// <summary>
        /// If enabled the core scene is a dependency of every other scene, and is never unloaded.
        /// </summary>
        public bool LoadCoreScene => _loadCoreScene;
        public SceneRef CoreScene => _coreScene;

        /// <summary>
        /// The slot the first loaded scene occupies when the game starts.
        /// </summary>
        public SlotId InitialSlot => _initialSlot;

        public IReadOnlyList<SlotDefinition> Slots => _slots;
        public IReadOnlyList<SceneDefinition> Scenes => _scenes;

        /// <summary>
        /// Guid of the core scene if it is enabled and assigned, otherwise null.
        /// </summary>
        public string ActiveCoreGuid => _loadCoreScene && _coreScene.IsValid ? _coreScene.Guid : null;

        // Mutators used by tests and tooling. Regular editing goes through SerializedObject.
        internal void SetCore(bool load, SceneRef core)
        {
            _loadCoreScene = load;
            _coreScene = core;
        }

        internal void SetInitialSlot(SlotId slot)
        {
            _initialSlot = slot;
        }

        internal SlotDefinition AddSlot(string key)
        {
            var slot = new SlotDefinition(_nextSlotId++, key);
            _slots.Add(slot);
            return slot;
        }

        internal SceneDefinition AddScene(SceneRef scene, params SceneRef[] dependencies)
        {
            var definition = new SceneDefinition(scene, dependencies);
            _scenes.Add(definition);
            return definition;
        }

        public bool IsRegistered(string guid)
        {
            return FindScene(guid) != null;
        }

        public SceneDefinition FindScene(string guid)
        {
            for (int i = 0; i < _scenes.Count; i++)
                if (_scenes[i].Scene.Guid == guid) return _scenes[i];
            return null;
        }

        /// <summary>
        /// Refresh the cached paths and names of all scene references from their guids.
        /// </summary>
        public void RefreshCaches(Func<string, (string path, string name)> resolve)
        {
            _coreScene = SceneDefinition.Refresh(_coreScene, resolve);
            for (int i = 0; i < _scenes.Count; i++) _scenes[i].RefreshCaches(resolve);
        }

        /// <summary>
        /// Create a deep copy.
        /// </summary>
        public ScenesData Clone()
        {
            return JsonUtility.FromJson<ScenesData>(JsonUtility.ToJson(this));
        }

        public bool IsSameAs(ScenesData other)
        {
            return other != null && JsonUtility.ToJson(this) == JsonUtility.ToJson(other);
        }
    }
}
