namespace Arosoul.Transitions
{
    /// <summary>
    /// Entry point for playing transitions. Works without Scenes.
    /// </summary>
    public static class TransitionAPI
    {
        /// <summary>
        /// True while a transition is playing.
        /// </summary>
        public static bool IsPlaying => TransitionManager.HasInstance && TransitionManager.Inst.ActiveHandle != null;

        /// <summary>
        /// Get the active transition. Returns null if no transition is playing.
        /// </summary>
        public static TransitionHandle GetActive()
        {
            return TransitionManager.HasInstance ? TransitionManager.Inst.ActiveHandle : null;
        }

        /// <summary>
        /// Play a transition with the default times of its definition.
        /// </summary>
        /// <returns>Handle used to follow the phases and control the flow of the transition.</returns>
        public static TransitionHandle Play(TransitionId id)
        {
            return Play(TransitionParameters.Default(id));
        }

        /// <summary>
        /// Play a transition.
        /// If a transition is already active, the new one only starts if it has a higher priority.
        /// Otherwise the returned handle is already cancelled.
        /// </summary>
        public static TransitionHandle Play(TransitionParameters parameters)
        {
            return TransitionManager.Inst.Play(parameters);
        }
    }
}
