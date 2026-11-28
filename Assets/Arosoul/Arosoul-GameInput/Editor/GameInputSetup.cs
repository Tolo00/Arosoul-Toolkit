using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Arosoul.GameInput.EditorTools {

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

        [MenuItem("Tools/Arosoul/GameInput/Create Config", priority = 102)]
        static void CreateConfig() => GameInputAgentTools.CreateConfig();
    }

}
