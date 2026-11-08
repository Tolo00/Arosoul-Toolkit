using UnityEngine;

public class ItemUtility : MonoBehaviour
{
    public static Item Spawn(ItemData item, Vector3 position)
    {
        Debug.Assert(item != null, "ItemData is null.");

        var obj = Instantiate(item.Prefab);
        SetPosition(obj, position);

        if (obj.TryGetComponent(out Item component))
        {
            return component;
        }

        Debug.LogError("Item component could not be found on item prefab. Deleting...");
        Destroy(obj);
        return null;
    }

    private static void SetPosition(GameObject obj, Vector3 position)
    {
        if (obj.TryGetComponent(out Rigidbody rigidbody) && !rigidbody.isKinematic) rigidbody.MovePosition(position);
        else obj.transform.position = position;
    }
}
