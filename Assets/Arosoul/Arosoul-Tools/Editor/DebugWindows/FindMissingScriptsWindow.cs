using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class FindMissingScriptsWindow : EditorWindow
{
    private enum SearchScope { EntireProject, Folder, Scene }

    private SearchScope _searchScope = SearchScope.EntireProject;
    private string _selectedFolder = "Assets";
    private SceneAsset _selectedScene;
    private Vector2 _scrollPos;

    private struct MissingScriptEntry
    {
        public string PrefabOrScene;
        public string HierarchyPath;
        public GameObject Object;
    }

    private List<MissingScriptEntry> _results = new();
    private bool _hasSearched = false;

    [MenuItem("Tools/Arosoul/Utilities/Find Missing Scripts")]
    public static void Open()
    {
        var window = GetWindow<FindMissingScriptsWindow>(false, "Find Missing Scripts");
        window.minSize = new Vector2(400, 300);
    }

    void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Search Scope", EditorStyles.boldLabel);

        _searchScope = (SearchScope)GUILayout.Toolbar((int)_searchScope,
            new[] { "Entire Project", "Folder", "Scene" });

        EditorGUILayout.Space(4);

        if (_searchScope == SearchScope.Folder)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder:", GUILayout.Width(50));
            EditorGUILayout.LabelField(_selectedFolder, EditorStyles.helpBox);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", _selectedFolder, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    _selectedFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();
        }
        else if (_searchScope == SearchScope.Scene)
        {
            _selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                "Scene", _selectedScene, typeof(SceneAsset), false);
        }

        EditorGUILayout.Space(4);

        GUI.enabled = _searchScope != SearchScope.Scene || _selectedScene != null;
        if (GUILayout.Button("Find Missing Scripts", GUILayout.Height(28)))
            RunSearch();
        GUI.enabled = true;

        EditorGUILayout.Space(4);

        if (_hasSearched)
        {
            string summary = _results.Count == 0
                ? "No missing scripts found."
                : $"Found {_results.Count} missing script{(_results.Count == 1 ? "" : "s")}:";

            EditorGUILayout.LabelField(summary, EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var entry in _results)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(entry.PrefabOrScene, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(entry.HierarchyPath, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndVertical();

                    if (entry.Object != null)
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(32)))
                            EditorGUIUtility.PingObject(entry.Object);
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void RunSearch()
    {
        _results.Clear();
        _hasSearched = false;

        try
        {
            switch (_searchScope)
            {
                case SearchScope.EntireProject:
                    SearchPrefabs(Application.dataPath);
                    SearchAllScenes();
                    break;
                case SearchScope.Folder:
                    SearchPrefabs(_selectedFolder.Replace("Assets", Application.dataPath));
                    SearchScenesInFolder(_selectedFolder);
                    break;
                case SearchScope.Scene:
                    string path = AssetDatabase.GetAssetPath(_selectedScene);
                    SearchInScene(path);
                    break;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _hasSearched = true;
            Repaint();
        }
    }

    private void SearchPrefabs(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            string prefabPath = "Assets" + files[i].Substring(Application.dataPath.Length);
            if (EditorUtility.DisplayCancelableProgressBar(
                    "Searching Prefabs", prefabPath, (float)i / files.Length))
                break;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (go != null)
            {
                FindInGO(go, prefabPath);
                EditorUtility.UnloadUnusedAssetsImmediate(true);
            }
        }
    }

    private void SearchAllScenes()
    {
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        SearchSceneGuids(sceneGuids);
    }

    private void SearchScenesInFolder(string folder)
    {
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folder });
        SearchSceneGuids(sceneGuids);
    }

    private void SearchSceneGuids(string[] guids)
    {
        string activeScenePath = EditorSceneManager.GetActiveScene().path;

        for (int i = 0; i < guids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (EditorUtility.DisplayCancelableProgressBar(
                    "Searching Scenes", scenePath, (float)i / guids.Length))
                break;

            SearchInScene(scenePath);
        }

        if (!string.IsNullOrEmpty(activeScenePath))
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
    }

    private void SearchInScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
            FindInGO(root, scenePath);
    }

    private void FindInGO(GameObject go, string sourceName, string parentPath = "")
    {
        string path = string.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;

        foreach (var component in go.GetComponents<Component>())
        {
            if (component == null)
            {
                _results.Add(new MissingScriptEntry
                {
                    PrefabOrScene = sourceName,
                    HierarchyPath = path,
                    Object = go
                });
            }
        }

        foreach (Transform child in go.transform)
            FindInGO(child.gameObject, sourceName, path);
    }
}