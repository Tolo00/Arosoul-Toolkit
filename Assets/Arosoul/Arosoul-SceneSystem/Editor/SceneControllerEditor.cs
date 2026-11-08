using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arosoul.SceneSystem.Editor
{
    [CustomEditor(typeof(SceneManager))]
    public class SceneControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            SceneManager controller = (SceneManager)target;
            
            EditorGUILayout.Space(10);
            
            // Is Busy status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Is Busy");
            GUI.enabled = false;
            EditorGUILayout.Toggle(controller.IsBusy);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Loaded Scenes
            EditorGUILayout.LabelField("Loaded Scenes by Slot", EditorStyles.boldLabel);
            
            var loadedScenes = GetLoadedScenes(controller);
            
            if (loadedScenes.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenes currently loaded through Scene Controller.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                foreach (var kvp in loadedScenes)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Slot Key
                    EditorGUILayout.LabelField("Slot:", GUILayout.Width(35));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(kvp.Key, GUILayout.MinWidth(100));
                    GUI.enabled = true;
                    
                    // Arrow
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    
                    // Scene Name
                    EditorGUILayout.LabelField("Scene:", GUILayout.Width(45));
                    GUI.enabled = false;
                    EditorGUILayout.TextField(kvp.Value, GUILayout.MinWidth(100));
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private Dictionary<string, string> GetLoadedScenes(SceneManager controller)
        {
            // Access the private field using reflection
            var field = typeof(SceneManager).GetField("_loadedSceneBySlot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return field.GetValue(controller) as Dictionary<string, string>;
            }
            
            throw new System.Exception("Unable to find _loadedSceneBySlot using reflection.");
        }
    }
}