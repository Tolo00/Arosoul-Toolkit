using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_itemID"), new GUIContent("Item ID"));
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Add To Item Database"))
        {
            ((ItemData)target).AddToItemDatabase();
        }
        
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(20);

        base.OnInspectorGUI();
    }
}

[CustomEditor(typeof(ExampleItemData))]
public class PotionDataEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_itemID"), new GUIContent("Item ID"));
        EditorGUI.EndDisabledGroup();
        
        if (GUILayout.Button("Add To Item Database"))
        {
            ((ItemData)target).AddToItemDatabase();
        }
        
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(20);

        base.OnInspectorGUI();
    }
}