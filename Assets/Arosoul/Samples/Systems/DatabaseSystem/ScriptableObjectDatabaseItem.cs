using UnityEngine;
using SaintsField;
using SaintsField.Playa;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Base class for any ScriptableObject that belongs to a ScriptableObjectDatabase.
/// </summary>
public abstract class ScriptableObjectDatabaseItem<TDatabase> : ScriptableObject
    where TDatabase : ScriptableObject
{
    [SerializeField, ReadOnly] private int _id = -1;
    public int ID => _id;

#if UNITY_EDITOR

    private void OnEnable() => ValidateID();

    protected virtual void ValidateID()
    {
        if (_id == -1)
            Debug.LogWarning($"'{name}' has no valid ID. Add it to the database.", this);
    }

    /// <summary>
    /// WARNING: Only call this from ScriptableObjectDatabase.
    /// </summary>
    [System.Obsolete("This method should only be called by the ScriptableObjectDatabase class.")]
    public virtual void SetID(int newID)
    {
        if (Application.isPlaying) return;
        _id = newID;
    }

    [Button("Add To Database")]
    public void AddToDatabase()
    {
        if (Application.isPlaying) return;

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(TDatabase).Name}");
        if (guids.Length == 0)
        {
            Debug.LogWarning($"No database of type '{typeof(TDatabase).Name}' found.", this);
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var database = AssetDatabase.LoadAssetAtPath<TDatabase>(path) as IScriptableObjectDatabase;
        database?.AddItem(this);
    }

#endif
}