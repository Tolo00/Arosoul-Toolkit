using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arosoul.Scenes
{
    [Serializable]
    public class ScenePathEntry
    {
        public string Guid;
        public string Path;
        public string Name;
    }

    /// <summary>
    /// Runtime copy of Scenes configuration. Baked by the editor from the project settings and included in builds
    /// as a preloaded asset. Do not edit manually.
    /// </summary>
    public class SceneCatalog : ScriptableObject
    {
        [SerializeField] private ScenesData _data = new ScenesData();
        [SerializeField] private List<ScenePathEntry> _paths = new List<ScenePathEntry>();

        [NonSerialized] private SceneGraph _graph;
        [NonSerialized] private Dictionary<string, ScenePathEntry> _pathByGuid;
        [NonSerialized] private Dictionary<string, string> _guidByPath;

        public ScenesData Data => _data;

        public SceneGraph Graph => _graph ??= new SceneGraph(_data);

        private static SceneCatalog _instance;

        public static SceneCatalog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Find();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private static SceneCatalog Find()
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneCatalog)}");
            for (int i = 0; i < guids.Length; i++)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (catalog != null) return catalog;
            }
            return null;
#else
            var catalogs = Resources.FindObjectsOfTypeAll<SceneCatalog>();
            return catalogs.Length > 0 ? catalogs[0] : null;
#endif
        }

        private void OnEnable()
        {
            Invalidate();
        }

        private void Invalidate()
        {
            _graph = null;
            _pathByGuid = null;
            _guidByPath = null;
        }

        private void EnsureLookups()
        {
            if (_pathByGuid != null) return;
            _pathByGuid = new Dictionary<string, ScenePathEntry>();
            _guidByPath = new Dictionary<string, string>();
            for (int i = 0; i < _paths.Count; i++)
            {
                _pathByGuid[_paths[i].Guid] = _paths[i];
                _guidByPath[_paths[i].Path] = _paths[i].Guid;
            }
        }

        public bool TryGetPath(string guid, out string path)
        {
            EnsureLookups();
            if (guid != null && _pathByGuid.TryGetValue(guid, out ScenePathEntry entry))
            {
                path = entry.Path;
                return true;
            }
            path = null;
            return false;
        }

        public string GetName(string guid)
        {
            EnsureLookups();
            return guid != null && _pathByGuid.TryGetValue(guid, out ScenePathEntry entry) ? entry.Name : guid;
        }

        public bool TryGetGuidByPath(string path, out string guid)
        {
            EnsureLookups();
            return _guidByPath.TryGetValue(path, out guid);
        }

        public bool TryGetSlotKey(SlotId id, out string key)
        {
            for (int i = 0; i < _data.Slots.Count; i++)
            {
                if (_data.Slots[i].Id != id) continue;
                key = _data.Slots[i].Key;
                return true;
            }
            key = null;
            return false;
        }

        /// <summary>
        /// Replace the contents. Returns true if anything changed.
        /// </summary>
        internal bool Initialize(ScenesData data, IEnumerable<ScenePathEntry> paths)
        {
            var newPaths = new List<ScenePathEntry>(paths);
            bool changed = !_data.IsSameAs(data) || JsonUtility.ToJson(new PathList { Items = _paths }) != JsonUtility.ToJson(new PathList { Items = newPaths });

            _data = data;
            _paths = newPaths;
            Invalidate();
            return changed;
        }

        [Serializable]
        private class PathList
        {
            public List<ScenePathEntry> Items;
        }

        internal static void SetInstanceForTests(SceneCatalog catalog)
        {
            _instance = catalog;
        }
    }
}
