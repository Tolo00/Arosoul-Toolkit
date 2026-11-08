using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

public static class EditorShortcuts
{
    // ── Inspector Lock ──────────────────────────────────────────────────────
    [MenuItem("Tools/Arosoul/Shortcuts/Toggle Inspector Lock %l")]
    static void ToggleInspectorLock()
    {
        var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        var inspector = EditorWindow.GetWindow(inspectorType);
        var prop = inspectorType.GetProperty("isLocked",
            BindingFlags.Public | BindingFlags.Instance);

        if (prop == null) { Debug.LogWarning("Could not find isLocked property."); return; }

        bool current = (bool)prop.GetValue(inspector);
        prop.SetValue(inspector, !current);
        inspector.Repaint();
    }

    // ── Transform ──────────────────────────────────────────────────────

    // Move selected GameObject to world origin
    [MenuItem("Tools/Arosoul/Shortcuts/Move to World Origin %&o")]
    static void MoveToWorldOrigin()
    {
        foreach (var go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Move to World Origin");
            go.transform.position = Vector3.zero;
        }
    }

    [MenuItem("Tools/Arosoul/Shortcuts/Move to World Origin %&o", true)]
    static bool MoveToWorldOriginValidate() => Selection.gameObjects.Length > 0;

    // Reset entire transform
    [MenuItem("Tools/Arosoul/Shortcuts/Reset Transform %&r")]
    static void ResetTransform()
    {
        foreach (var go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Reset Transform");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
        }
    }

    [MenuItem("Tools/Arosoul/Shortcuts/Reset Transform %&r", true)]
    static bool ResetTransformValidate() => Selection.gameObjects.Length > 0;

    // ── Selection ──────────────────────────────────────────────────────────

    // Select all children of the selected GameObject
    [MenuItem("Tools/Arosoul/Shortcuts/Select All Children %#&c")]
    static void SelectAllChildren()
    {
        var go = Selection.activeGameObject;
        if (go == null) return;

        var children = new GameObject[go.transform.childCount];
        for (int i = 0; i < go.transform.childCount; i++)
            children[i] = go.transform.GetChild(i).gameObject;

        Selection.objects = children;
    }

    [MenuItem("Tools/Arosoul/Shortcuts/Select All Children %#&c", true)]
    static bool SelectAllChildrenValidate() =>
        Selection.activeGameObject != null &&
        Selection.activeGameObject.transform.childCount > 0;

    // Select the parent of the selected GameObject
    [MenuItem("Tools/Arosoul/Shortcuts/Select Parent %#&p")]
    static void SelectParent()
    {
        var go = Selection.activeGameObject;
        if (go?.transform.parent == null) return;
        Selection.activeGameObject = go.transform.parent.gameObject;
    }

    [MenuItem("Tools/Arosoul/Shortcuts/Select Parent %#&p", true)]
    static bool SelectParentValidate() =>
        Selection.activeGameObject?.transform.parent != null;

    // ── Editor Utilities ───────────────────────────────────────────────────

    // Clear the console log
    [MenuItem("Tools/Arosoul/Shortcuts/Clear Console %#k")]
    static void ClearConsole()
    {
        var logEntries = typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");
        logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public)
                   ?.Invoke(null, null);
    }

    // Save the project (assets + scenes)
    [MenuItem("Tools/Arosoul/Shortcuts/Save Project %&s")]
    static void SaveProject()
    {
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Project saved.");
    }
}