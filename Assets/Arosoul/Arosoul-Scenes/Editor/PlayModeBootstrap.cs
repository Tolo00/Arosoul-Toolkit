using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Makes sure the dependencies of a scene are loaded first when pressing play in the editor.
    ///
    /// When the open scene is registered and has dependencies, the editor starts an empty bootstrap scene instead.
    /// At startup the runtime loads the scene you were editing through Scenes (see SceneBootstrap),
    /// which loads core and dependencies in order before the scene itself.
    ///
    /// The start scene is only touched while it is unset or already ours, a start scene set by you is never replaced.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeBootstrap
    {
        public const string BootstrapScenePath = SceneCatalogBaker.GeneratedFolder + "/PlayModeBootstrap.unity";

        private static bool _updating;

        static PlayModeBootstrap()
        {
            EditorApplication.delayCall += UpdateRedirect;
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => UpdateRedirect();
            EditorSceneManager.sceneOpened += (_, __) => UpdateRedirect();
            EditorSceneManager.sceneSaved += _ => UpdateRedirect();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    UpdateRedirect();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    SceneCatalogBaker.BakePendingIfNeeded();
                    UpdateRedirect();
                    break;
            }
        }

        /// <summary>
        /// Decide whether play mode should start in the bootstrap scene, and set up the redirect accordingly.
        /// </summary>
        public static void UpdateRedirect()
        {
            if (_updating || EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            _updating = true;
            try
            {
                SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
                SceneAsset current = EditorSceneManager.playModeStartScene;
                bool isOurs = current != null && current == bootstrap;

                // Respect a start scene chosen by the user.
                if (current != null && !isOurs)
                {
                    SessionState.EraseString(SceneBootstrap.PlayTargetKey);
                    return;
                }

                if (!TryGetTarget(out string guid))
                {
                    if (isOurs) EditorSceneManager.playModeStartScene = null;
                    SessionState.EraseString(SceneBootstrap.PlayTargetKey);
                    return;
                }

                if (bootstrap == null) bootstrap = CreateBootstrapScene();
                EditorSceneManager.playModeStartScene = bootstrap;
                SessionState.SetString(SceneBootstrap.PlayTargetKey, guid);
            }
            finally
            {
                _updating = false;
            }
        }

        private static bool TryGetTarget(out string guid)
        {
            guid = null;
            var settings = ScenesSettings.instance;
            if (!settings.PlayModeBootstrap) return false;

            Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!active.IsValid() || string.IsNullOrEmpty(active.path) || active.path == BootstrapScenePath) return false;

            guid = AssetDatabase.AssetPathToGUID(active.path);
            if (!settings.Data.IsRegistered(guid)) return false;

            // Only worth redirecting if something has to be loaded before the scene.
            return new SceneGraph(settings.Data).GetDependencies(guid).Count > 0;
        }

        private static SceneAsset CreateBootstrapScene()
        {
            Directory.CreateDirectory(SceneCatalogBaker.GeneratedFolder);

            Scene previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded)
                EditorSceneManager.SetActiveScene(previous);

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        }
    }
}
