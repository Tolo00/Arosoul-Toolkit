using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Starts Scenes when the game starts.
    ///
    /// In the editor, pressing play in a scene that has dependencies starts a small empty bootstrap scene instead (see the Scene Config window).
    /// The bootstrap loads the scene you were editing through Scenes, so dependencies are loaded first, in order.
    ///
    /// In builds, and in the editor without bootstrap, the scene Unity loads is adopted into the initial slot and its missing dependencies
    /// are loaded after it. Awake order between the first scene and its dependencies is then not guaranteed.
    /// For deterministic ordering in builds, make the first scene in the build a minimal boot scene and load the rest through Scenes.
    /// </summary>
    internal static class SceneBootstrap
    {
        internal const string PlayTargetKey = "Arosoul.Scenes.PlayTarget";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void OnAfterSceneLoad()
        {
            // Nothing configured, Scenes stays dormant.
            if (SceneCatalog.Instance == null) return;

            try
            {
                SceneManager manager = SceneManager.Inst;

#if UNITY_EDITOR
                string target = UnityEditor.SessionState.GetString(PlayTargetKey, "");
                if (!string.IsNullOrEmpty(target))
                {
                    UnityEditor.SessionState.EraseString(PlayTargetKey);
                    await manager.RunEditorPlayTargetAsync(target);
                    return;
                }
#endif
                await manager.AdoptInitialSceneAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
