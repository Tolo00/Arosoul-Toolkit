namespace Arosoul.Commands
{
    /// <summary>
    /// One line in the console log.
    /// </summary>
    public sealed class CommandLogEntry
    {
        public CommandLogEntry(string message, CommandLogType type, string stackTrace)
        {
            Message = message ?? string.Empty;
            Type = type;
            StackTrace = stackTrace;
        }

        public string Message { get; }
        public CommandLogType Type { get; }

        /// <summary>
        /// Stack trace of an error, or null.
        /// </summary>
        public string StackTrace { get; }

        public override string ToString() => Message;
    }
}
