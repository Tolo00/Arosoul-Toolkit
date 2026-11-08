using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Arosoul.ContextFramework.Editor
{
    [CustomEditor(typeof(RuntimeSetBase<>), true)]
    public class RuntimeSetEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {

            IRuntimeSet runtimeSet = (IRuntimeSet)target;
            if (EditorApplication.isPlaying) {

                IReadOnlyList<Object> itemObjects = runtimeSet.ItemObjects;
                if (runtimeSet.ItemObjects == null || runtimeSet.ItemObjects.Count == 0) {
                    EditorGUILayout.LabelField("Set is empty.");
                }
                else {
                    EditorGUI.BeginDisabledGroup(true); // Make read-only
                    for (int i = 0; i < itemObjects.Count; i++)
                    {
                        Object obj = itemObjects[i];
                        EditorGUILayout.ObjectField($"Element {i}", obj, runtimeSet.SetType, allowSceneObjects: true);
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
            else {
                EditorGUILayout.HelpBox("Enter playmode to show items inside runtime set.", MessageType.Info);
            }
        }
    }
}