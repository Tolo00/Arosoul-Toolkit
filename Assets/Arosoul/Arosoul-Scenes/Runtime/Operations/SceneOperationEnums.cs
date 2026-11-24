namespace Arosoul.Scenes
{
    public enum SceneOperationState
    {
        Created,
        Queued,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public enum SceneOperationPhase
    {
        None,
        Planning,
        TransitionIn,
        Unloading,
        Loading,
        Initializing,
        UnloadingAssets,
        TransitionOut,
        Done
    }
}
