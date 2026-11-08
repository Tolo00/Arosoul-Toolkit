using UnityEngine;
using UnityEditor;

namespace Arosoul.ContextFramework.Editor
{
    [CustomEditor(typeof(GameEventListener))]
    [CanEditMultipleObjects]
    public class GameEventListenerEditor : UnityEditor.Editor {
        private SerializedProperty _eventProp;
        private SerializedProperty _responseProp;

        void OnEnable() {
            _eventProp = serializedObject.FindProperty("Event");
            _responseProp= serializedObject.FindProperty("Response");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_eventProp);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_responseProp);
        
            serializedObject.ApplyModifiedProperties();
        }
    }

}