using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GameInputSystem.EditorTools {

    [InitializeOnLoad]
    internal static class GameInputSetup {
        // Lets other packages (like the Player Settings integration) compile only when Game Input is present.
        const string Define = "GAMEINPUT_INSTALLED";

        static readonly NamedBuildTarget[] Targets = {
            NamedBuildTarget.Standalone, NamedBuildTarget.iOS, NamedBuildTarget.Android, NamedBuildTarget.WebGL, NamedBuildTarget.tvOS
        };

        static GameInputSetup() => EditorApplication.delayCall += EnsureDefine;

        static void EnsureDefine() {
            foreach (var target in Targets) {
                var symbols = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => s.Length > 0).ToList();
                if (symbols.Contains(Define)) continue;

                symbols.Add(Define);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            }
        }

        [MenuItem("Tools/Game Input/Create Config")]
        static void CreateConfig() {
            var existing = GameActionsGenerator.FindConfig();
            if (existing != null) {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Directory.CreateDirectory("Assets/Resources");
            var config = ScriptableObject.CreateInstance<GameInputConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Resources/" + GameInputConfig.ResourceName + ".asset");
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }

}
