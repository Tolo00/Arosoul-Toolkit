using System;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Entry point for game code. Values read here are the applied values.
    /// Prefer the generated CustomSettings class for your own settings, it wraps these calls type safely.
    /// </summary>
    public static class GameSettings {
        static SettingsStore store;

        /// <summary>An applied value changed. Argument is the setting id.</summary>
        public static event Action<string> Changed;

        /// <summary>A batch of settings was applied.</summary>
        public static event Action Applied;

        public static SettingsStore Store {
            get {
                if (store == null) Initialize(new JsonFileSettingsStorage());
                return store;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() {
            store = null;
            Changed = null;
            Applied = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() {
            Initialize(new JsonFileSettingsStorage());
        }

        /// <summary>Loads and applies all settings. Called automatically, call it yourself to use a different storage.</summary>
        public static void Initialize(ISettingsStorage storage) {
            if (store != null) store.Flush();

            SettingsRegistry.Rebuild();
            store = new SettingsStore(SettingsRegistry.All, storage);
            store.Changed += id => Changed?.Invoke(id);
            store.Applied += () => Applied?.Invoke();

            SettingsRunner.Attach(store);
            store.Load();
        }

        // Reading / writing

        public static T Get<T>(string id) {
            object value = Store.GetApplied(id);
            if (value is T typed) return typed;

            if (typeof(T).IsEnum && value is string name && Enum.IsDefined(typeof(T), name)) {
                return (T)Enum.Parse(typeof(T), name);
            }

            Debug.LogWarning($"[Settings] Could not read '{id}' as {typeof(T).Name}.");
            return default;
        }

        public static float GetFloat(string id) => Get<float>(id);
        public static int GetInt(string id) => Get<int>(id);
        public static bool GetBool(string id) => Get<bool>(id);
        public static string GetString(string id) => Get<string>(id);

        /// <summary>Sets and applies a value immediately. Enum settings accept a C# enum value or the option id.</summary>
        public static void Set(string id, object value) => Store.Set(id, value);

        // Pending edits (what the menu uses)
        public static bool HasPending => Store.HasPending;
        public static void Apply() => Store.ApplyPending();
        public static void Revert() => Store.RevertPending();
        public static void ResetToDefaults(bool applyImmediately = true) => Store.ResetToDefaults(applyImmediately);
    }

    /// <summary>Hidden object that writes changes to disk (debounced) and on pause/quit.</summary>
    [AddComponentMenu("")]
    internal sealed class SettingsRunner : MonoBehaviour {
        static SettingsRunner instance;
        SettingsStore store;
        float nextSave;

        public static void Attach(SettingsStore store) {
            if (!Application.isPlaying) return;

            if (instance == null) {
                var go = new GameObject("[GameSettings]") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SettingsRunner>();
            }
            instance.store = store;
        }

        void LateUpdate() {
            if (store == null || !store.SaveRequested || Time.unscaledTime < nextSave) return;
            nextSave = Time.unscaledTime + 0.5f;
            store.Flush();
        }

        void OnApplicationPause(bool paused) { if (paused) store?.Flush(); }
        void OnApplicationQuit() { store?.Flush(); }
        void OnDestroy() { store?.Flush(); }
    }

}
