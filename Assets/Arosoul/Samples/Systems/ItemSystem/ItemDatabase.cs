using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase _instance;
    
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                
                #if UNITY_EDITOR
                if (_instance == null)
                {
                    Debug.LogWarning("ItemDatabase not found in Resources folder!");
                }
                #endif
            }
            return _instance;
        }
    }
    
    [SerializeField, ReadOnly] private List<ItemData> _itemDataAssets;
    
    public IReadOnlyList<ItemData> ItemDataAssets => _itemDataAssets;
    
    public ItemData GetItemById(int id)
    {
        return _itemDataAssets.FirstOrDefault(item => item.ID == id);
    }

#if UNITY_EDITOR
    
    [Button()]
    private void AddNewItemsToDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

        int itemsAdded = 0;
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (so is ItemData itemData)
            {
                if (_itemDataAssets.Contains(itemData)) 
                    continue;

                AddItemToDatabase(itemData);
                itemsAdded++;
            }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"{itemsAdded} items was added to the database.");
    }
    
    [Button()]
    private void RemoveNullReferences()
    {
        int count = 0;
        for (int i = _itemDataAssets.Count-1; i >= 0; i--)
        {
            if (_itemDataAssets[i] == null)
            {
                _itemDataAssets.RemoveAt(i);
                count++;
            }
        }

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"{count} null references was removed from the database.");
    }

    public void AddItemToDatabase(ItemData itemData)
    {
        if (_itemDataAssets.Contains(itemData))
        {
            Debug.LogWarning("Item is already in database.", this);
            return;
        }

        itemData.SetItemID(_itemDataAssets.Count);
        _itemDataAssets.Add(itemData);

        EditorUtility.SetDirty(itemData);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"{itemData.name} was added to database.");
    }

    [Button()]
    private void ClearDatabase()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Clear Database",
            "This action will remove all items from the database.\nWhen items are re-added, they might not have the same ID.\nContinue?",
            "Yes",
            "No"
        );

        if (proceed)
        {
            foreach (ItemData data in _itemDataAssets)
            {
                data.SetItemID(-1); // id not set

                EditorUtility.SetDirty(data);
            }
            _itemDataAssets.Clear();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}