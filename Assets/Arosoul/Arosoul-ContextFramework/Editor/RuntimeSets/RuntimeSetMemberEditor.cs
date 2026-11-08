using UnityEditor;
using UnityEngine;

namespace Arosoul.ContextFramework.Editor
{
    [CustomEditor(typeof(RuntimeSetMember))]
    [CanEditMultipleObjects]
    public class RuntimeSetMemberEditor : UnityEditor.Editor {
        SerializedProperty _runtimeSetInterfaceProp;
        SerializedProperty _registrationModeProp;

        void OnEnable() {
            _runtimeSetInterfaceProp = serializedObject.FindProperty("_runtimeSetObject");
            _registrationModeProp = serializedObject.FindProperty("_registrationMode");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_runtimeSetInterfaceProp, new GUIContent("Runtime Set"));
            EditorGUILayout.PropertyField(_registrationModeProp);

            DisplayCompatibilityWarning();

            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayCompatibilityWarning() {
            var runtimeSetObject = _runtimeSetInterfaceProp.objectReferenceValue;
            if (runtimeSetObject == null) return;

            var runtimeSetMember = target as RuntimeSetMember;
            if (runtimeSetMember == null) return;

            System.Type setType = GetRuntimeSetGenericType(runtimeSetObject);    
            if (setType == null) return;

            bool hasRequiredType = false;
            if (setType == typeof(GameObject)) {
                hasRequiredType = true; // GameObject always has itself
            }
            else if (typeof(Component).IsAssignableFrom(setType)) {
                hasRequiredType = runtimeSetMember.GetComponent(setType) != null;
            }

            if (!hasRequiredType) {
                EditorGUILayout.HelpBox(
                    $"This GameObject does not have a component of type '{setType.Name}' which is required by the RuntimeSet. " +
                    $"Add a '{setType.Name}' component to this GameObject.",
                    MessageType.Warning
                );
            }
        }

        private System.Type GetRuntimeSetGenericType(Object runtimeSetObject) {
            if (runtimeSetObject == null) return null;

            System.Type objectType = runtimeSetObject.GetType();

            // Find RuntimeSetBase<T> in the inheritance hierarchy
            while (objectType != null && objectType != typeof(object)) {
                if (objectType.IsGenericType && 
                    objectType.GetGenericTypeDefinition() == typeof(RuntimeSetBase<>)) {
                    // Get the generic type argument (T)
                    return objectType.GetGenericArguments()[0];
                }
                objectType = objectType.BaseType;
            }

            return null;
        }
    }
}