
namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// Implement this interface on a ScriptableObject to receive a callback
    /// when the Unity Editor enters Play Mode.
    /// </summary>
    /// <remarks>
    /// These callbacks only work inside the Unity Editor.  
    /// For runtime builds, use <see cref="MonoBehaviour.OnEnable"/> or similar lifecycle methods.
    /// </remarks>
    public interface IEnterPlayModeCallback {
        void OnEnterPlayMode();
    }

    /// <summary>
    /// Implement this interface on a ScriptableObject to receive a callback
    /// when the Unity Editor exits Play Mode.
    /// </summary>
    /// <remarks>
    /// These callbacks only work inside the Unity Editor.  
    /// For runtime builds, use <see cref="MonoBehaviour.OnDisable"/> or similar lifecycle methods.
    /// </remarks>
    public interface IExitPlayModeCallback {
        void OnExitPlayMode();
    }
}