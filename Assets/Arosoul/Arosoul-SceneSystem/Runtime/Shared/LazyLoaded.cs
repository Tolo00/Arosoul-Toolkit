using UnityEngine;

namespace Arosoul.SceneSystem
{
    /// <summary>
    /// Lazy loaded instance of a MonoBehaviour component.
    /// When it's required, an instance will be created on a new GameObject that is marked as DontDestroyOnLoad.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class LazyLoaded<T> : MonoBehaviour where T : MonoBehaviour {
        private static T inst;
        public static T Inst { 
            get {
                if (inst != null) return inst;
                
                // Lazy loading - Create instance if none exists
                GameObject lazyInst = new GameObject($"[{typeof(T).Name}]");
                inst = lazyInst.AddComponent<T>();
                return inst;
            } 
            private set => inst = value; 
        }

        protected void OnEnable() {
            if (inst != null && inst != this) { 
                if (gameObject != null)
                    Destroy(gameObject);
                return;
            }
            inst = this as T;

            transform.SetParent(null); // Unparent
            DontDestroyOnLoad(gameObject);
            
            OnSingletonEnable();
        }

        protected void OnDisable() {
            if (inst == this) {
                OnSingletonDisable();
                inst = null;
            }
        }

        // Force intitialize singleton
        public static void Initialize() {
            _ = Inst;
        }

        // Override these to ensure the singleton is initialized correctly
        protected virtual void OnSingletonEnable() { }
        protected virtual void OnSingletonDisable() { }
    }
}