namespace Arosoul.Transitions
{
    /// <summary>
    /// Describes how a transition should be played.
    /// Immutable, use the With methods to create modified copies.
    /// </summary>
    public readonly struct TransitionParameters
    {
        public const float FallbackInTime = 0.5f;
        public const float FallbackMinHoldTime = 0.2f;
        public const float FallbackOutTime = 0.5f;

        public readonly TransitionId Id;
        public readonly float InTime;
        public readonly float MinHoldTime;
        public readonly float OutTime;

        /// <summary>
        /// A transition can only interrupt an active transition if its priority is higher.
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// If true the transition stays in its Hold phase until <see cref="TransitionHandle.Release"/> is called.
        /// </summary>
        public readonly bool HoldUntilReleased;

        public TransitionParameters(TransitionId id, float inTime, float minHoldTime, float outTime, int priority = 0, bool holdUntilReleased = false)
        {
            Id = id;
            InTime = inTime;
            MinHoldTime = minHoldTime;
            OutTime = outTime;
            Priority = priority;
            HoldUntilReleased = holdUntilReleased;
        }

        /// <summary>
        /// Parameters using the default times configured on the transition definition.
        /// </summary>
        public static TransitionParameters Default(TransitionId id)
        {
            if (TransitionRegistry.TryGetDefinition(id, out TransitionDefinition definition))
                return new TransitionParameters(id, definition.DefaultInTime, definition.DefaultMinHoldTime, definition.DefaultOutTime);

            return new TransitionParameters(id, FallbackInTime, FallbackMinHoldTime, FallbackOutTime);
        }

        /// <summary>
        /// Same time for in and out.
        /// </summary>
        public static TransitionParameters Symmetric(TransitionId id, float fadeTime, float minHoldTime = 0.2f)
        {
            return new TransitionParameters(id, fadeTime, minHoldTime, fadeTime);
        }

        public TransitionParameters WithTimes(float inTime, float minHoldTime, float outTime)
        {
            return new TransitionParameters(Id, inTime, minHoldTime, outTime, Priority, HoldUntilReleased);
        }

        public TransitionParameters WithPriority(int priority)
        {
            return new TransitionParameters(Id, InTime, MinHoldTime, OutTime, priority, HoldUntilReleased);
        }

        public TransitionParameters WithHoldUntilReleased(bool hold = true)
        {
            return new TransitionParameters(Id, InTime, MinHoldTime, OutTime, Priority, hold);
        }

        public static implicit operator TransitionParameters(TransitionId id) => Default(id);
    }
}
