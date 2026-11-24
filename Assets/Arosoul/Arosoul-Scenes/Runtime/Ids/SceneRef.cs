using System;
using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Serializable reference to a scene. Use it instead of a string field, it is drawn as a scene picker in the inspector.
    /// The scene is identified by its asset GUID, so renaming or moving the scene never breaks the reference.
    /// </summary>
    [Serializable]
    public struct SceneRef : IEquatable<SceneRef>
    {
        [SerializeField] private string _guid;
        [SerializeField] private string _path;
        [SerializeField] private string _name;

        public SceneRef(string guid, string path = "", string name = "")
        {
            _guid = guid;
            _path = path;
            _name = name;
        }

        public static SceneRef FromGuid(string guid) => new SceneRef(guid);

        public string Guid => _guid ?? "";
        public string Path => _path ?? "";

        /// <summary>
        /// Display name of the scene. Cached by the editor.
        /// </summary>
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name)) return _name;
                return string.IsNullOrEmpty(_path) ? "" : System.IO.Path.GetFileNameWithoutExtension(_path);
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(_guid);

        internal SceneRef WithCache(string path, string name)
        {
            return new SceneRef(_guid, path, name);
        }

        public bool Equals(SceneRef other) => Guid == other.Guid;
        public override bool Equals(object obj) => obj is SceneRef other && Equals(other);
        public override int GetHashCode() => Guid.GetHashCode();

        public static bool operator ==(SceneRef a, SceneRef b) => a.Equals(b);
        public static bool operator !=(SceneRef a, SceneRef b) => !a.Equals(b);

        public override string ToString() => IsValid ? (Name.Length > 0 ? Name : _guid) : "None";
    }
}
