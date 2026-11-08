using UnityEngine;

namespace Arosoul.Essentials 
{
    /// <summary>
    /// Cross-scene persistent singleton. The instance persists for the lifetime of the application
    /// and is never destroyed on scene load. No lazy loading, the object MUST be explicitly created before use.
    ///
    /// Use when: you need a global manager that always exists, such as an audio manager, input handler, or save system. 
    /// If the instance is accessed before the persistent scene has loaded, a clear error is logged rather than silently creating a broken instance.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class SingletonPersistent<T> : MonoBehaviour where T : MonoBehaviour {
        public static T Inst { get; private set; }
        public static bool IsInitialized { get; private set; }

        protected void OnEnable() {
            if (Inst != null) {
                Destroy(gameObject);
                return;
            }

            if (this is not T self) {
                Debug.LogError($"[SingletonPersistent] {GetType().Name} does not match type parameter {typeof(T).Name}.");
                return;
            }

            Inst = self;
            IsInitialized = true;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            OnSingletonEnable();
        }

        protected void OnDisable() {
            if (Inst == this as T) {
                OnSingletonDisable();
                Inst = null;
                IsInitialized = false;
            }
        }

        protected virtual void OnSingletonEnable() { }
        protected virtual void OnSingletonDisable() { }
    }
}