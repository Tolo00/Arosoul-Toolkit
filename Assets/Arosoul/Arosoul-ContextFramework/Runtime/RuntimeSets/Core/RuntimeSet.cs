using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// A generic collection that maintains a unique, ordered set of items at runtime.
    /// 
    /// Combines a HashSet for fast lookup with a List for stable iteration and indexing.
    /// Commonly used within the Context Framework to track active objects (e.g., enemies,
    /// interactables, players) in a scene.
    /// 
    /// Provides methods for adding, removing, iterating, and selecting random elements,
    /// while preventing duplicates.
    /// </summary>
    [Serializable]
    public class RuntimeSet<T> {
        private readonly HashSet<T> _set = new();
        [SerializeField] private List<T> _list = new();
        
        private readonly System.Random _random = new();

        /// <summary>
        /// Read-only list of items in set.
        /// </summary>
        public IReadOnlyList<T> Items => _list;

        /// <summary>
        /// Number of elements in the set.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Indexer to access by order of insertion.
        /// </summary>
        public T this[int i] => _list[i];

        /// <summary>
        /// Add an item if not already present.
        /// Returns whether successful.
        /// </summary>
        public bool Add(T item) {
            if (_set.Add(item)) {
                _list.Add(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an item if present.
        /// Returns whether successful.
        /// </summary>
        public bool Remove(T item) {
            if (_set.Remove(item)) {
                _list.Remove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get a random item from the set.
        /// </summary>
        public T GetRandom() {
            if (_list.Count == 0) throw new InvalidOperationException("Set is empty");
            int index = _random.Next(_list.Count);
            return _list[index];
        }

        /// <summary>
        /// Get the first item added (by insertion order).
        /// </summary>
        public T GetFirstAdded() {
            if (_list.Count == 0) throw new InvalidOperationException("Set is empty");
            return _list[0];
        }

        /// <summary>
        /// Get the last item added (by insertion order).
        /// </summary>
        public T GetLastAdded()
        {
            if (_list.Count == 0) throw new InvalidOperationException("Set is empty");
            return _list[^1];
        }

        /// <summary>
        /// Execute action on all items in set.
        /// </summary>
        public void DoAll(Action<T> action) {
            for (int i = Items.Count-1; i >= 0; i--) {
                action?.Invoke(Items[i]);
            }
        }

        /// <summary>
        /// Clear all items.
        /// </summary>
        public void Clear()
        {
            _set.Clear();
            _list.Clear();
        }

        /// <summary>
        /// Enumerator support.
        /// </summary>
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    }
}
