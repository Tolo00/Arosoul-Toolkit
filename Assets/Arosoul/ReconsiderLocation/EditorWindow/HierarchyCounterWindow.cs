using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class HierarchyCounterWindow : EditorWindow
{
    private enum GroupBy { Tag, Layer }

    private struct CountEntry
    {
        public string Name;
        public int Total;
        public int Active;
        public int Inactive;
    }

    private int _totalCount;
    private int _activeCount;
    private int _inactiveCount;
    private GroupBy _groupBy = GroupBy.Tag;
    private List<CountEntry> _groups = new();
    private Vector2 _scrollPos;

    [MenuItem("Tools/Arosoul/Utilities/Hierarchy Counter")]
    public static void Open()
    {
        var window = GetWindow<HierarchyCounterWindow>(false, "Hierarchy Counter");
        window.minSize = new Vector2(340, 300);
    }

    void OnEnable()
    {
        EditorApplication.hierarchyChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        EditorApplication.hierarchyChanged -= Refresh;
    }

    void OnGUI()
    {
        EditorGUILayout.Space(4);

        // Summary
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        DrawRow("Total Objects",    _totalCount,    bold: true);
        DrawRow("  Active",         _activeCount);
        DrawRow("  Inactive",       _inactiveCount);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // Group by toolbar
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Group By", GUILayout.Width(65));
        EditorGUI.BeginChangeCheck();
        _groupBy = (GroupBy)GUILayout.Toolbar((int)_groupBy, new[] { "Tag", "Layer" });
        if (EditorGUI.EndChangeCheck()) Refresh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Column headers
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField(_groupBy.ToString(), EditorStyles.toolbarButton);
        EditorGUILayout.LabelField("Total",    EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.LabelField("Active",   EditorStyles.toolbarButton, GUILayout.Width(55));
        EditorGUILayout.LabelField("Inactive", EditorStyles.toolbarButton, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // Group rows
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        foreach (var group in _groups)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(group.Name);
            EditorGUILayout.LabelField(group.Total.ToString(),    GUILayout.Width(50));
            EditorGUILayout.LabelField(group.Active.ToString(),   GUILayout.Width(55));
            EditorGUILayout.LabelField(group.Inactive.ToString(), GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Refresh"))
            Refresh();
        EditorGUILayout.Space(4);
    }

    private void Refresh()
    {
        _groups.Clear();
        _totalCount = _activeCount = _inactiveCount = 0;

        var allObjects = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                CollectAll(root, allObjects);
        }

        _totalCount   = allObjects.Count;
        _activeCount   = allObjects.Count(go => go.activeInHierarchy);
        _inactiveCount = _totalCount - _activeCount;

        var groupDict = new Dictionary<string, CountEntry>();
        foreach (var go in allObjects)
        {
            string key = _groupBy == GroupBy.Tag ? go.tag : LayerMask.LayerToName(go.layer);
            if (!groupDict.TryGetValue(key, out var entry))
                entry = new CountEntry { Name = key };

            entry.Total++;
            if (go.activeInHierarchy) entry.Active++;
            else entry.Inactive++;
            groupDict[key] = entry;
        }

        _groups = groupDict.Values.OrderByDescending(e => e.Total).ToList();
        Repaint();
    }

    private void CollectAll(GameObject go, List<GameObject> list)
    {
        list.Add(go);
        foreach (Transform child in go.transform)
            CollectAll(child.gameObject, list);
    }

    private void DrawRow(string label, int value, bool bold = false)
    {
        var style = bold ? EditorStyles.boldLabel : EditorStyles.label;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, style);
        EditorGUILayout.LabelField(value.ToString(), style, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
    }
}