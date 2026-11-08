using UnityEngine;
using UnityEditor;

namespace Arosoul.Essentials.Editor {

    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.PropertyField(position, property, label);

            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null) {
                GUILayout.Space(30);

                Rect warningRect = new Rect(position.x, position.y + position.height * 1.3f, position.width, position.height * 1.3f);
                EditorGUI.HelpBox(warningRect, "This field is required!", MessageType.Error);
            }
        }
    }
}
