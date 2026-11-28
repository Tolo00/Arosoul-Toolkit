using UnityEditor;
using UnityEngine;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Adds the Commands Console prefab to the open scene.
    /// </summary>
    public static class CommandsConsoleCreator
    {
        public const string PrefabName = "Commands Console";

        [MenuItem("GameObject/Arosoul/Commands Console", false, 10)]
        private static void CreateFromMenu()
        {
            Selection.activeGameObject = AddConsoleToActiveScene();
        }

        /// <summary>
        /// Instantiate the Commands Console prefab in the active scene, or return the console that is already there.
        /// </summary>
        public static GameObject AddConsoleToActiveScene()
        {
            var existing = Object.FindAnyObjectByType<CommandConsole>(FindObjectsInactive.Include);
            if (existing != null) return existing.gameObject;

            GameObject prefab = FindPrefab();
            GameObject instance = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(PrefabName, typeof(CommandConsole));

            Undo.RegisterCreatedObjectUndo(instance, "Create Commands Console");
            return instance;
        }

        /// <summary>
        /// The Commands Console prefab shipped with the package, or null if it can not be found.
        /// </summary>
        public static GameObject FindPrefab()
        {
            foreach (string guid in AssetDatabase.FindAssets($"\"{PrefabName}\" t:Prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.TryGetComponent(out CommandConsole _)) return prefab;
            }
            return null;
        }
    }
}
