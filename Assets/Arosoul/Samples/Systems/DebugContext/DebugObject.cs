using UnityEngine;

/// <summary>
/// Attach this component to an object that you only want to exist when development mode is active.
/// </summary>
public class DebugObject : MonoBehaviour
{
    void Start()
    {
        gameObject.SetActive(DebugContext.DebugObjectsEnabled);
    }
}
