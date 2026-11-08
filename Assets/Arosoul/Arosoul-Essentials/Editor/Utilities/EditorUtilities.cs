using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arosoul.Essentials.Editor {

    public static class EditorUtilities {
        public static string GetNameOfAsset(Object asset) {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string assetName = Path.GetFileName(assetPath);
            return assetName.Split('.')[0];
        }

        public static void DrawHorizontalLine(Color color, float thickness = 2) {
            Rect rect = GUILayoutUtility.GetRect(0f, thickness); // Get a rect of height 2
            EditorGUI.DrawRect(rect, color); // Draw the line (gray color)
        }

        public static void DrawDefaultInspectorExcept(string fieldToSkip, SerializedObject serializedObject) {
            SerializedProperty prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false)) {
                if (prop.name != fieldToSkip)
                    EditorGUILayout.PropertyField(prop, true);
            }
            serializedObject.ApplyModifiedProperties();
        }
    
        public static void DrawTitle(string title) {
            // Create a style for the header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;                // bigger font
            headerStyle.alignment = TextAnchor.UpperLeft; 

            // Draw the header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, headerStyle);
            EditorGUILayout.Space();
        }
    }
}
