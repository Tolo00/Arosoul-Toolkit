using UnityEditor;
using UnityEngine;

namespace GameInputSystem.EditorTools {

    [CustomEditor(typeof(GameInputConfig))]
    internal sealed class GameInputConfigEditor : Editor {
        public override void OnInspectorGUI() {
            var config = (GameInputConfig)target;

            DrawDefaultInspector();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(config.Actions == null)) {
                if (GUILayout.Button("Open Input Actions Editor")) AssetDatabase.OpenAsset(config.Actions);
                if (GUILayout.Button("Generate Actions")) GameActionsGenerator.Generate(true);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Edit actions in the Input Actions editor, GameActions is regenerated on save.\n" +
                "Button actions: expected type Button. Vector2 / Axis actions: set the expected control type.\n" +
                "Name an action 'SomethingPriority' to get a PriorityAction.",
                MessageType.None);
        }
    }

}
