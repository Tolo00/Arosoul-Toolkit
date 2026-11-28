namespace Arosoul.Commands
{
    /// <summary>
    /// Minimum level of Unity log messages (Debug.Log, warnings, errors) mirrored into the console.
    /// </summary>
    public enum UnityLogLevel
    {
        /// <summary>Do not mirror Unity log messages.</summary>
        None,

        /// <summary>Errors, exceptions and failed asserts.</summary>
        Error,

        /// <summary>Warnings and everything above.</summary>
        Warning,

        /// <summary>Every message.</summary>
        Log
    }
}
