namespace Arosoul.Commands
{
    /// <summary>
    /// Which objects an instance command (a non-static member of a MonoBehaviour) runs on.
    /// </summary>
    public enum MonoTargetType
    {
        /// <summary>The first active instance found in the loaded scenes. With several instances, which one is found is not defined.</summary>
        First,

        /// <summary>Every active instance in the loaded scenes.</summary>
        All
    }
}
