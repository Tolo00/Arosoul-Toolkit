using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefsEditorWindow : EditorWindow
{
    private enum PrefsType { PlayerPrefs, EditorPrefs }
    private enum ValueType { String, Int, Float }

    private struct PrefEntry
    {
        public string Key;
        public string RawValue;
        public ValueType Type;
        public bool IsEditing;
    }

    private PrefsType _prefsType = PrefsType.PlayerPrefs;
    private List<PrefEntry> _entries = new();
    private Vector2 _scrollPos;
    private string _searchFilter = "";

    // Add new pref fields
    private string _newKey = "";
    private string _newValue = "";
    private ValueType _newType = ValueType.String;

    private string _statusMessage = "";
    private double _statusClearTime;

    [MenuItem("Tools/Arosoul/Utilities/Prefs Editor")]
    public static void Open()
    {
        var window = GetWindow<PrefsEditorWindow>(false, "Prefs Editor");
        window.minSize = new Vector2(480, 400);
    }

    void OnEnable() => Refresh();

    void OnGUI()
    {
        // Toolbar
        EditorGUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        _prefsType = (PrefsType)GUILayout.Toolbar((int)_prefsType,
            new[] { "PlayerPrefs", "EditorPrefs" });
        if (EditorGUI.EndChangeCheck())
            Refresh();

        EditorGUILayout.Space(4);

        // Search + Refresh
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        if (GUILayout.Button("Refresh", GUILayout.Width(65)))
            Refresh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Column headers
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Key", EditorStyles.toolbarButton, GUILayout.Width(160));
        EditorGUILayout.LabelField("Type", EditorStyles.toolbarButton, GUILayout.Width(55));
        EditorGUILayout.LabelField("Value", EditorStyles.toolbarButton);
        EditorGUILayout.LabelField("", EditorStyles.toolbarButton, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        // Entry list
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!string.IsNullOrEmpty(_searchFilter) &&
                !entry.Key.ToLower().Contains(_searchFilter.ToLower()))
                continue;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.LabelField(entry.Key, GUILayout.Width(160));
            EditorGUILayout.LabelField(entry.Type.ToString(), GUILayout.Width(55));

            if (entry.IsEditing)
            {
                entry.RawValue = EditorGUILayout.TextField(entry.RawValue);
                _entries[i] = entry;

                if (GUILayout.Button("Save", GUILayout.Width(42)))
                {
                    SaveEntry(entry);
                    entry.IsEditing = false;
                    _entries[i] = entry;
                    SetStatus("Saved: " + entry.Key);
                }
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    entry.IsEditing = false;
                    _entries[i] = entry;
                }
            }
            else
            {
                EditorGUILayout.LabelField(entry.RawValue);
                if (GUILayout.Button("Edit", GUILayout.Width(42)))
                {
                    entry.IsEditing = true;
                    _entries[i] = entry;
                }
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    if (EditorUtility.DisplayDialog("Delete Pref",
                        $"Delete \"{entry.Key}\"?", "Delete", "Cancel"))
                    {
                        DeleteEntry(entry.Key);
                        SetStatus("Deleted: " + entry.Key);
                        Refresh();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Add new entry
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _newKey   = EditorGUILayout.TextField(_newKey,   GUILayout.Width(160));
        _newType  = (ValueType)EditorGUILayout.EnumPopup(_newType, GUILayout.Width(65));
        _newValue = EditorGUILayout.TextField(_newValue);
        GUI.enabled = !string.IsNullOrWhiteSpace(_newKey);
        if (GUILayout.Button("Add", GUILayout.Width(42)))
        {
            AddEntry(_newKey, _newValue, _newType);
            SetStatus("Added: " + _newKey);
            _newKey = _newValue = "";
            Refresh();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Delete all
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.color = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button($"Delete All {_prefsType}", GUILayout.Width(160)))
        {
            if (EditorUtility.DisplayDialog("Delete All",
                $"Delete ALL {_prefsType}? This cannot be undone.", "Delete All", "Cancel"))
            {
                if (_prefsType == PrefsType.PlayerPrefs) PlayerPrefs.DeleteAll();
                else EditorPrefs.DeleteAll();
                SetStatus($"Deleted all {_prefsType}.");
                Refresh();
            }
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        // Status bar
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            if (EditorApplication.timeSinceStartup > _statusClearTime)
                _statusMessage = "";
            else
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        EditorGUILayout.Space(4);
    }

    private void Refresh()
    {
        _entries.Clear();
        var keys = _prefsType == PrefsType.PlayerPrefs
            ? GetPlayerPrefsKeys()
            : GetEditorPrefsKeys();

        foreach (var key in keys)
        {
            var entry = new PrefEntry { Key = key };
            DetectAndReadValue(key, ref entry);
            _entries.Add(entry);
        }

        Repaint();
    }

    private void DetectAndReadValue(string key, ref PrefEntry entry)
    {
        bool isPlayer = _prefsType == PrefsType.PlayerPrefs;

        // Probe type by attempting to read sentinel values
        // Int: GetInt returns 0 by default, but we check if SetInt was used
        // by checking if the float and string defaults don't match
        // Best-effort heuristic: try int → float → string
        string strVal  = isPlayer ? PlayerPrefs.GetString(key, "\u0002") : EditorPrefs.GetString(key, "\u0002");
        float  floatVal = isPlayer ? PlayerPrefs.GetFloat(key, float.NaN) : EditorPrefs.GetFloat(key, float.NaN);
        int    intVal   = isPlayer ? PlayerPrefs.GetInt(key, int.MinValue) : EditorPrefs.GetInt(key, int.MinValue);

        if (strVal != "\u0002")
        {
            entry.Type = ValueType.String;
            entry.RawValue = strVal;
        }
        else if (!float.IsNaN(floatVal) && floatVal != int.MinValue)
        {
            // Ambiguous between int and float — prefer int if value is whole
            if (floatVal == Mathf.Floor(floatVal) && intVal != int.MinValue)
            {
                entry.Type = ValueType.Int;
                entry.RawValue = intVal.ToString();
            }
            else
            {
                entry.Type = ValueType.Float;
                entry.RawValue = floatVal.ToString("G");
            }
        }
        else
        {
            entry.Type = ValueType.Int;
            entry.RawValue = intVal.ToString();
        }
    }

    private void SaveEntry(PrefEntry entry)
    {
        bool isPlayer = _prefsType == PrefsType.PlayerPrefs;
        switch (entry.Type)
        {
            case ValueType.String:
                if (isPlayer) PlayerPrefs.SetString(entry.Key, entry.RawValue);
                else          EditorPrefs.SetString(entry.Key, entry.RawValue);
                break;
            case ValueType.Int:
                if (int.TryParse(entry.RawValue, out int i))
                {
                    if (isPlayer) PlayerPrefs.SetInt(entry.Key, i);
                    else          EditorPrefs.SetInt(entry.Key, i);
                }
                break;
            case ValueType.Float:
                if (float.TryParse(entry.RawValue, out float f))
                {
                    if (isPlayer) PlayerPrefs.SetFloat(entry.Key, f);
                    else          EditorPrefs.SetFloat(entry.Key, f);
                }
                break;
        }
        if (isPlayer) PlayerPrefs.Save();
    }

    private void DeleteEntry(string key)
    {
        if (_prefsType == PrefsType.PlayerPrefs) PlayerPrefs.DeleteKey(key);
        else                                     EditorPrefs.DeleteKey(key);
        if (_prefsType == PrefsType.PlayerPrefs) PlayerPrefs.Save();
    }

    private void AddEntry(string key, string value, ValueType type)
    {
        bool isPlayer = _prefsType == PrefsType.PlayerPrefs;
        switch (type)
        {
            case ValueType.String:
                if (isPlayer) PlayerPrefs.SetString(key, value);
                else          EditorPrefs.SetString(key, value);
                break;
            case ValueType.Int:
                int.TryParse(value, out int i);
                if (isPlayer) PlayerPrefs.SetInt(key, i);
                else          EditorPrefs.SetInt(key, i);
                break;
            case ValueType.Float:
                float.TryParse(value, out float f);
                if (isPlayer) PlayerPrefs.SetFloat(key, f);
                else          EditorPrefs.SetFloat(key, f);
                break;
        }
        if (isPlayer) PlayerPrefs.Save();
    }

    private void SetStatus(string msg)
    {
        _statusMessage = msg;
        _statusClearTime = EditorApplication.timeSinceStartup + 3.0;
    }

    // ── Platform-specific key enumeration ──────────────────────────────────

    private List<string> GetPlayerPrefsKeys()
    {
        var keys = new List<string>();
#if UNITY_EDITOR_WIN
        using var root = Registry.CurrentUser.OpenSubKey(
            $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}");
        if (root != null)
            foreach (var name in root.GetValueNames())
                keys.Add(name.Contains("_h") ? name.Substring(0, name.LastIndexOf("_h")) : name);
#elif UNITY_EDITOR_OSX
        string plistPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}" +
                           $"/Library/Preferences/unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist";
        if (System.IO.File.Exists(plistPath))
        {
            // Use plutil to convert to XML and parse keys
            var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo("plutil",
                $"-convert xml1 -o - \"{plistPath}\"")
            { RedirectStandardOutput = true, UseShellExecute = false };
            proc.Start();
            string xml = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            var dictNode = doc.SelectSingleNode("//dict");
            if (dictNode != null)
                foreach (System.Xml.XmlNode node in dictNode.ChildNodes)
                    if (node.Name == "key") keys.Add(node.InnerText);
        }
#else
        // Linux: ~/.config/unity3d/CompanyName/ProductName/prefs (XML)
        string prefsPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}" +
                           $"/.config/unity3d/{PlayerSettings.companyName}/{PlayerSettings.productName}/prefs";
        if (System.IO.File.Exists(prefsPath))
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load(prefsPath);
            foreach (System.Xml.XmlNode node in doc.SelectNodes("//pref"))
            {
                var attr = node.Attributes?["name"];
                if (attr != null) keys.Add(attr.Value);
            }
        }
#endif
        return keys;
    }

    private List<string> GetEditorPrefsKeys()
    {
        var keys = new List<string>();
#if UNITY_EDITOR_WIN
        using var root = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Unity Editor 5.x");
        if (root != null)
            foreach (var name in root.GetValueNames())
                keys.Add(name.Contains("_h") ? name.Substring(0, name.LastIndexOf("_h")) : name);
#elif UNITY_EDITOR_OSX
        string plistPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}" +
                           "/Library/Preferences/com.unity3d.UnityEditor5.x.plist";
        if (System.IO.File.Exists(plistPath))
        {
            var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo("plutil",
                $"-convert xml1 -o - \"{plistPath}\"")
            { RedirectStandardOutput = true, UseShellExecute = false };
            proc.Start();
            string xml = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);
            var dictNode = doc.SelectSingleNode("//dict");
            if (dictNode != null)
                foreach (System.Xml.XmlNode node in dictNode.ChildNodes)
                    if (node.Name == "key") keys.Add(node.InnerText);
        }
#else
        string prefsPath = $"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)}" +
                           "/.config/unity3d/EditorPrefs.plist";
        if (System.IO.File.Exists(prefsPath))
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load(prefsPath);
            foreach (System.Xml.XmlNode node in doc.SelectNodes("//pref"))
            {
                var attr = node.Attributes?["name"];
                if (attr != null) keys.Add(attr.Value);
            }
        }
#endif
        return keys;
    }
}