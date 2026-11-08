using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// A ScriptableObject wrapper around <see cref="RuntimeSet{T}"/>, used to create
    /// persistent runtime sets as project assets.
    /// 
    /// Enables global, scene-independent tracking of active game objects or components
    /// that register themselves at runtime (e.g., all enemies, all active cameras).
    /// 
    /// Automatically clears its contents when entering or exiting Play Mode in the Editor
    /// to ensure clean runtime states.
    /// </summary>
    public abstract class RuntimeSetBase<T> : ScriptableObject, IEnterPlayModeCallback, IExitPlayModeCallback, IRuntimeSet where T : Object {
        [SerializeField]
        private RuntimeSet<T> _runtimeSet = new RuntimeSet<T>();

        /// <summary>
        /// Read-only list of items in set.
        /// </summary>
        public IReadOnlyList<T> Items => _runtimeSet.Items;

        /// <summary>
        /// Number of elements in the set.
        /// </summary>
        public int Count => _runtimeSet.Count;


        /// <summary>
        /// Indexer to access by order of insertion.
        /// </summary>
        public T this[int i] => _runtimeSet[i];

        /// <summary>
        /// Indexer to access by order of insertion.
        /// </summary>
        public T GetValue(int i) => this[i];


        /// <summary>
        /// Add an item if not already present.
        /// Returns whether successful.
        /// </summary>
        public bool Add(T item) => _runtimeSet.Add(item);

        /// <summary>
        /// Remove an item if present.
        /// Returns whether successful.
        /// </summary>
        public bool Remove(T item) => _runtimeSet.Remove(item);

        /// <summary>
        /// Get a random item from the set.
        /// </summary>
        public T GetRandom() => _runtimeSet.GetRandom();

        /// <summary>
        /// Get the first item added (by insertion order).
        /// </summary>
        public T GetFirstAdded() => _runtimeSet.GetFirstAdded();

        /// <summary>
        /// Get the last item added (by insertion order).
        /// </summary>
        public T GetLastAdded() => _runtimeSet.GetLastAdded();

        /// <summary>
        /// Execute action on all items in set.
        /// </summary>
        public void DoAll(System.Action<T> action) => _runtimeSet.DoAll(action);
        
        /// <summary>
        /// Clear all items in set.
        /// </summary>
        public void Clear() => _runtimeSet.Clear();



        void OnEnable() {
            OnEnterPlayMode();
        }

        public void OnEnterPlayMode() {
            Clear();
        }

        public void OnExitPlayMode() {
            Clear();
        }

        // IRuntimeSet implementation
        public void AddObject(Object obj) {
            if (obj is T t) Add(t);
        }

        public void RemoveObject(Object obj) {
            if (obj is T t) Remove(t);
        }

        public virtual System.Type SetType => typeof(T);
        public IReadOnlyList<Object> ItemObjects => Items;
    }
}