using UnityEngine;
using UnityEngine.Pool;

namespace Arosoul.Essentials
{
    /// <summary>
    /// Generic wrapper around Unity's built-in ObjectPool for easier usage.
    /// </summary>
    public class ComponentPool<T> where T : Component {

        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;

        /// <summary>
        /// Creates a new object pool.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate when the pool needs new objects.</param>
        /// <param name="parent">Optional parent transform for pooled objects.</param>
        /// <param name="prewarmCount">How many objects to pre-instantiate.</param>
        /// <param name="collectionCheck">Whether to check for double release calls (debug only).</param>
        /// <param name="defaultCapacity">Default starting capacity of the pool.</param>
        /// <param name="maxSize">Maximum allowed pool size.</param>
        public ComponentPool(
            T prefab,
            Transform parent = null,
            int prewarmCount = 0,
            bool collectionCheck = false,
            int defaultCapacity = 10,
            int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<T>(
                createFunc: CreateItem,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroy,
                collectionCheck: collectionCheck,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            Prewarm(prewarmCount);
        }

        /// <summary>
        /// Gets an instance from the pool.
        /// </summary>
        public T Get() => _pool.Get();

        /// <summary>
        /// Releases an instance back into the pool.
        /// </summary>
        public void Release(T item) => _pool.Release(item);

        /// <summary>
        /// Pre-instantiates a number of pooled objects.
        /// </summary>
        public void Prewarm(int count) {
            if (count <= 0) return;

            var temp = new T[count];
            for (int i = 0; i < count; i++)
                temp[i] = _pool.Get();

            for (int i = 0; i < count; i++)
                _pool.Release(temp[i]);
        }

        private T CreateItem() {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGet(T item) {
            item.gameObject.SetActive(true);
        }

        private void OnRelease(T item) {
            item.gameObject.SetActive(false);
        }

        private void OnDestroy(T item) {
            if (item != null)
                UnityEngine.Object.Destroy(item.gameObject);
        }
    }
}