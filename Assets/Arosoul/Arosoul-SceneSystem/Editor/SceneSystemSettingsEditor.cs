using UnityEditor;
using UnityEngine;

namespace Arosoul.SceneSystem.Editor
{
    [CustomEditor(typeof(SceneSystemSettings))]
    public class SceneSystemSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.HelpBox(
                "Scene System Settings are managed in Project Settings.",
                MessageType.Info
            );

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Project Settings", GUILayout.Height(30), GUILayout.Width(200)))
            {
                SettingsService.OpenProjectSettings("Project/Player/Scene System");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }
    }
}
