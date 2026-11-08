using UnityEngine;
using UnityEditor;

namespace Arosoul.Essentials.Editor {

    [CustomPropertyDrawer(typeof(ForceInterfaceAttribute))]
    public class ForceInterfaceAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType == SerializedPropertyType.ObjectReference) {
                ForceInterfaceAttribute forceAttribute = (ForceInterfaceAttribute)attribute;
                EditorGUI.BeginProperty(position, label, property);

                    EditorGUI.BeginChangeCheck();
                        Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(Object), !EditorUtility.IsPersistent(property.serializedObject.targetObject));

                    // Only run checks if obj property has been updated
                    // Prevents accidental setting of property when multiple objects are selected
                    if (EditorGUI.EndChangeCheck()) {
                        if (obj == null) property.objectReferenceValue = null;
                        else if (forceAttribute.interfaceType.IsAssignableFrom(obj.GetType())) {
                            // obj is implementing interfaceType
                            property.objectReferenceValue = obj;
                        }
                        else if (obj is GameObject) {
                            MonoBehaviour component = (MonoBehaviour)((GameObject)obj).GetComponent(forceAttribute.interfaceType);
                            if (component != null) {
                                // obj is a GameObject that contains a script that implements interfaceType
                                property.objectReferenceValue = component;
                            }
                        }
                    }

                EditorGUI.EndProperty();
            }
            else {
                EditorGUI.LabelField(position, "ERROR: ForceInterfaceAttribure should be used on type 'Object'");
            }
        }
    }

}