using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SaintsField;
using Arosoul.Essentials;
using SaintsField.Playa;

#if UNITY_EDITOR
using UnityEditor;

public interface IScriptableObjectDatabase
{
    void AddItem(ScriptableObject item);
}
#endif

/// <summary>
/// Generic ScriptableObject database. 
/// Inherit from this base class and place the scriptable object instance inside of the Resources folder.
/// </summary>
/// <typeparam name="TItem">The scriptable object type this database tracks.</typeparam>
/// <typeparam name="TDatabase">The class that inherits from this base class.</typeparam>
public abstract class ScriptableObjectDatabase<TItem, TDatabase> : ScriptableObjectSingleton<ScriptableObjectDatabase<TItem, TDatabase>>
#if UNITY_EDITOR
    ,IScriptableObjectDatabase
#endif
    where TItem : ScriptableObjectDatabaseItem<TDatabase>
    where TDatabase : ScriptableObjectDatabase<TItem, TDatabase>
{
    [SerializeField, ReadOnly] private List<TItem> _items = new();

    public IReadOnlyList<TItem> Items => _items;
    public int Count => _items.Count;

    public TItem GetByID(int id) {
        return _items.FirstOrDefault(item => item.ID == id) ?? throw new System.IndexOutOfRangeException($"ID '{id}' does not exist in database '{GetType().Name}'");
    }
    public TItem this[int id] => GetByID(id);

#if UNITY_EDITOR

    [Button("Add New Items")]
    private void AddNewItemsToDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
        int added = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (so is TItem item)
            {
                if (_items.Contains(item)) continue;
                AddItem(item);
                added++;
            }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"{added} items added to {GetType().Name}.");
    }

    [Button("Remove Null References")]
    private void RemoveNullReferences()
    {
        int count = 0;
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] == null) { _items.RemoveAt(i); count++; }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"{count} null references removed from {GetType().Name}.");
    }

    public void AddItem(TItem item)
    {
        if (_items.Contains(item))
        {
            Debug.LogWarning("Item is already in database.", this);
            return;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        item.SetID(_items.Count);
#pragma warning restore CS0618 // Type or member is obsolete
        _items.Add(item);

        EditorUtility.SetDirty(item);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"{item.name} added to {GetType().Name}.");
    }

    [Button("Clear")]
    private void ClearDatabase()
    {
        bool proceed = EditorUtility.DisplayDialog(
            $"Clear {GetType().Name}",
            "This will remove all items. IDs may not be preserved when re-added.\nContinue?",
            "Yes", "No"
        );

        if (!proceed) return;

        foreach (TItem item in _items)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            item.SetID(-1);
#pragma warning restore CS0618 // Type or member is obsolete
            EditorUtility.SetDirty(item);
        }

        _items.Clear();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    void IScriptableObjectDatabase.AddItem(ScriptableObject item) => AddItem((TItem)item);
#endif
}