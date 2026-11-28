namespace Arosoul.Commands
{
    public enum CommandLogType
    {
        /// <summary>A command typed by the user.</summary>
        Input,

        /// <summary>Command output and Unity log messages.</summary>
        Output,

        Warning,
        Error
    }
}
