using Arosoul.Essentials.Editor;
using UnityEditor;
using UnityEngine;

namespace Arosoul.ContextFramework.Editor 
{
    [CustomEditor(typeof(VariableBase<>), true)]
    [CanEditMultipleObjects]
    public class VariableBaseEditor : UnityEditor.Editor
    {
        SerializedProperty _developerDescription;

        SerializedProperty _runtimeValueProp;
        SerializedProperty _initialValueProp;
        SerializedProperty _readOnlyProp;
        SerializedProperty _applyOnExitProp;

        private void OnEnable() {
            _developerDescription = serializedObject.FindProperty("_developerDescription");

            _runtimeValueProp = serializedObject.FindProperty("_runtimeValue");
            _initialValueProp = serializedObject.FindProperty("_initialValue");
            _readOnlyProp = serializedObject.FindProperty("_readOnly");
            _applyOnExitProp = serializedObject.FindProperty("_applyOnExit");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorUtilities.DrawTitle(target.name);

            EditorGUILayout.PropertyField(_developerDescription);

            EditorGUILayout.Space();

            if (EditorApplication.isPlaying) EditorGUILayout.PropertyField(_runtimeValueProp, new GUIContent("Value"));
            else EditorGUILayout.PropertyField(_initialValueProp, new GUIContent("Value"));

            EditorGUILayout.PropertyField(_readOnlyProp);

            EditorGUILayout.Space();

            if (EditorApplication.isPlaying) {
                GUI.backgroundColor = _applyOnExitProp.boolValue ? Color.green : Color.gray;
                if (GUILayout.Button("Apply On Exit")) {
                    _applyOnExitProp.boolValue = !_applyOnExitProp.boolValue;
                }
                GUI.backgroundColor = Color.white;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }

}