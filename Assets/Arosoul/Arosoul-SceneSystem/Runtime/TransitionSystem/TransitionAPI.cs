
namespace Arosoul.SceneSystem
{
    public static class TransitionAPI
    {
        /// <summary>
        /// Get the active transition.
        /// Return null if no transition is running.
        /// </summary>
        public static TransitionOperation GetActive()
        {
            return TransitionManager.Inst.ActiveOperation;
        }

        /// <summary>
        /// Start a transition.
        /// </summary>
        /// <returns>TransitionOperation instance. Use it to subscribe to callbacks and control transition flow.</returns>
        public static TransitionOperation StartTransition(TransitionParameters parameters) 
        {
            var operation = new TransitionOperation()
            {
                Key = parameters.TransitionKey,

                InTime = parameters.InTime,
                MinWaitTime = parameters.MinWaitTime,
                OutTime = parameters.OutTime
            };
            TransitionManager.Inst.StartTransition(operation);
            return operation;
        }

        /// <summary>
        /// Start a transition.
        /// </summary>
        /// <returns>TransitionOperation instance. Use it to subscribe to callbacks and control transition flow.</returns>
        public static TransitionOperation StartTransition(TransitionParameters parameters, int priority) 
        {
            var operation = new TransitionOperation()
            {
                Key = parameters.TransitionKey,

                InTime = parameters.InTime,
                MinWaitTime = parameters.MinWaitTime,
                OutTime = parameters.OutTime,

                Priority = priority
            };
            TransitionManager.Inst.StartTransition(operation);
            return operation;
        }
    }
}
