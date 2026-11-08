using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Scriptable Objects/Item/Simple Item")]
public class ItemData : ScriptableObject
{
    [SerializeField, HideInInspector] private int _itemID = -1;
    public int ID => _itemID;

    [Header("Basic Info")]
    public string ItemName;
    [TextArea(3, 10)] public string Description;
    [Space]
    public Sprite Icon;
    public GameObject Prefab;

#if UNITY_EDITOR

    void OnEnable()
    {
        ValidateID();
    }

    protected virtual void ValidateID()
    {
        if (_itemID == -1)
        {
            // ID not set
            Debug.LogWarning($"Item '{name}' does not have a valid item ID. Add this item to the database to avoid unexpected behaviour.", this);
        }
    }

    /// <summary>
    /// WARNING: Do not set the ID of an Item!
    /// This method is meant to only be used by ItemDatabase. 
    /// </summary>
    public virtual void SetItemID(int newID)
    {
        if (Application.isPlaying) return;
        _itemID = newID;
    }

    public void AddToItemDatabase()
    {
        ItemDatabase.Instance.AddItemToDatabase(this);
    }

#endif
}
