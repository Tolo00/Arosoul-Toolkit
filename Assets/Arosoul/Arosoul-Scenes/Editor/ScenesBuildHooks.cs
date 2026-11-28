using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Bakes and validates Scenes before every build. Errors fail the build.
    /// </summary>
    internal class ScenesBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -90;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = ScenesSettings.instance;
            if (settings.Data.Scenes.Count == 0 && !settings.Data.LoadCoreScene) return; // not used in this project

            ValidationReport validation = SceneCatalogBaker.Bake();
            if (SceneCatalogBaker.IsIdsOutOfSync())
                Debug.LogWarning("The generated SceneIds script is out of sync with the Scene Config. Rebuild it in Tools > Arosoul > Scenes > Scene Config.");

            if (!validation.HasErrors) return;

            var messages = new System.Text.StringBuilder("Scenes configuration has errors:\n");
            foreach (var issue in validation.Issues)
                if (issue.Severity == ValidationSeverity.Error) messages.Append(" - ").Append(issue.Message).Append('\n');
            throw new BuildFailedException(messages.ToString());
        }
    }

    /// <summary>
    /// Keeps the catalog up to date when scenes are added, renamed, moved or deleted.
    /// </summary>
    internal class SceneAssetPostprocessor : AssetPostprocessor
    {
        private static bool _scheduled;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!HasScene(imported) && !HasScene(deleted) && !HasScene(moved) && !HasScene(movedFrom)) return;
            if (ScenesSettings.instance.Data.Scenes.Count == 0) return;
            if (_scheduled) return;

            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                SceneCatalogBaker.Bake();
                PlayModeBootstrap.UpdateRedirect();
            };
        }

        private static bool HasScene(string[] paths)
        {
            foreach (string path in paths)
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) && path != PlayModeBootstrap.BootstrapScenePath) return true;
            return false;
        }
    }
}
