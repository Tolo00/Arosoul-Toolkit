using UnityEditor;
using UnityEngine;

namespace Arosoul.ContextFramework.Editor 
{
    /// <summary>
    /// Provides automatic callbacks for ScriptableObjects that implement
    /// <see cref="IEnterPlayModeCallback"/> or <see cref="IExitPlayModeCallback"/>.
    /// 
    /// This utility runs *only in the Unity Editor* and hooks into the play mode
    /// state change event to notify any ScriptableObjects about entering or exiting play mode.
    /// 
    /// It enables ScriptableObjects to perform setup or cleanup logic
    /// without requiring a MonoBehaviour in the scene.
    /// </summary>
    [InitializeOnLoad]
    public static class ContextCallbacksHandler {

        static ContextCallbacksHandler() {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state) {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                // --- ENTER PLAYMODE CALLBACK ---
                // Triggered when Unity transitions from Edit Mode to Play Mode.
                // Useful for preparing data, initializing state, or resetting values.
                if (so is IEnterPlayModeCallback enterCallback) {
                    if (state == PlayModeStateChange.ExitingEditMode) {
                        enterCallback.OnEnterPlayMode();
                    }
                }

                // --- EXIT PLAYMODE CALLBACK ---
                // Triggered when Unity transitions from Play Mode back to Edit Mode.
                // Useful for cleanup, saving data, or restoring editor-only state.
                if (so is IExitPlayModeCallback exitCallback) {
                    if (state == PlayModeStateChange.ExitingPlayMode) {
                        exitCallback.OnExitPlayMode();
                    }
                }
            }
        }
    
    }

}