using UnityEngine;

namespace Arosoul.Essentials 
{
    /// <summary>
    /// Scene-scoped singleton. Only one instance can be active at once.
    /// Duplicate instances are destroyed in OnEnable. The instance is cleared when disabled/unloaded.
    ///
    /// Use when: you need a single manager that belongs to a specific scene (e.g. a level's wave spawner, UI controller, or camera rig). 
    /// Do NOT use for objects that need to persist across scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
        public static T Inst { get; private set; }

        protected void OnEnable() {
            if (Inst != null) {
                Destroy(gameObject);
                return;
            }

            if (this is not T self) {
                Debug.LogError($"[Singleton] {GetType().Name} does not match type parameter {typeof(T).Name}.");
                return;
            }

            Inst = self;
            OnSingletonEnable();
        }

        protected void OnDisable() {
            if (Inst == this as T) {
                OnSingletonDisable();
                Inst = null;
            }
        }

        protected virtual void OnSingletonEnable() { }
        protected virtual void OnSingletonDisable() { }
    }
}