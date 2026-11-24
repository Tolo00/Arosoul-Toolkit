using UnityEngine;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Lazy loaded instance of a MonoBehaviour component.
    /// When it's required, an instance will be created on a new GameObject that is marked as DontDestroyOnLoad.
    /// Derived classes must clear the static state in a [RuntimeInitializeOnLoadMethod(SubsystemRegistration)]
    /// method by calling <see cref="ClearInstance"/>, so the singleton works with domain reload disabled.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class LazyLoaded<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _inst;
        private static bool _quitting;

        /// <summary>
        /// True if an instance exists. Does not create one.
        /// </summary>
        public static bool HasInstance => _inst != null;

        public static T Inst
        {
            get
            {
                if (_inst != null) return _inst;
                if (_quitting) return null;

                // Lazy loading - Create instance if none exists
                GameObject lazyInst = new GameObject($"[{typeof(T).Name}]");
                _inst = lazyInst.AddComponent<T>();
                return _inst;
            }
        }

        /// <summary>
        /// Clear all static state. Call from a SubsystemRegistration method.
        /// </summary>
        protected static void ClearInstance()
        {
            _inst = null;
            _quitting = false;
        }

        protected void OnEnable()
        {
            if (_inst != null && _inst != this)
            {
                if (gameObject != null)
                    Destroy(gameObject);
                return;
            }
            _inst = this as T;

            transform.SetParent(null); // Unparent
            DontDestroyOnLoad(gameObject);

            OnSingletonEnable();
        }

        protected void OnDisable()
        {
            if (_inst == this)
            {
                OnSingletonDisable();
                _inst = null;
            }
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }

        // Force initialize singleton
        public static void Initialize()
        {
            _ = Inst;
        }

        // Override these to ensure the singleton is initialized correctly
        protected virtual void OnSingletonEnable() { }
        protected virtual void OnSingletonDisable() { }
    }
}
