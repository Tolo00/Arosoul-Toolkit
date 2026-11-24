namespace Arosoul.Transitions
{
    /// <summary>
    /// Phases of a transition. The order is meaningful: a later phase is always greater than an earlier one.
    /// </summary>
    public enum TransitionPhase
    {
        Pending = 0,
        In = 1,
        Hold = 2,
        Out = 3,
        Completed = 4,
        Cancelled = 5
    }
}
