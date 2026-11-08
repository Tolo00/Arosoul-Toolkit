using UnityEngine;
using UnityEditor;

namespace Arosoul.Essentials.Editor {

    [CustomPropertyDrawer(typeof(EnumArrayContainer<,>))]
    public class EnumArrayContainerDrawer : PropertyDrawer
    {
        private readonly float LINE_HEIGHT = EditorGUIUtility.singleLineHeight;
        private readonly float ELEMENT_SPACING = EditorGUIUtility.singleLineHeight/6;

        private SerializedProperty content;
        private SerializedProperty enumType;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (content == null)
                content = property.FindPropertyRelative("content");
            if (enumType == null)
                enumType = property.FindPropertyRelative("enumType"); 
            
            // Set height of property
            float totalHeight = LINE_HEIGHT;
            if (property.isExpanded) {
                // Resize array
                if (content.arraySize != enumType.enumNames.Length) {
                    content.arraySize = enumType.enumNames.Length;
                }
                
                // Increase height for each element
                for (int i = 0; i < content.arraySize; i++) {
                    totalHeight += EditorGUI.GetPropertyHeight(content.GetArrayElementAtIndex(i));
                    totalHeight += ELEMENT_SPACING;
                }
            }

            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (content == null)
                content = property.FindPropertyRelative("content");
            if (enumType == null)
                enumType = property.FindPropertyRelative("enumType");

            EditorGUI.BeginProperty(position, label, property);

            // Expand/Collapse foldout
            Rect foldoutRect = new Rect(position.x, position.y, position.width, LINE_HEIGHT);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            // Draw elements in array
            if (property.isExpanded) {
                EditorGUI.indentLevel++; // Indent all child properties

                float addY = LINE_HEIGHT;
                for (int i = 0; i < content.arraySize; i++) {
                    Rect rect = new Rect(position.x, position.y + addY, position.width, EditorGUI.GetPropertyHeight(content.GetArrayElementAtIndex(i)));
                    addY += rect.height;
                    addY += ELEMENT_SPACING;
                    // Draw element
                    EditorGUI.PropertyField(rect, content.GetArrayElementAtIndex(i), new GUIContent(enumType.enumNames[i]), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

}