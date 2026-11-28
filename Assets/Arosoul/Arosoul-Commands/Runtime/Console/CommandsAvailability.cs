namespace Arosoul.Commands
{
    /// <summary>
    /// Where the console is available. It is always available in the editor.
    /// </summary>
    public enum CommandsAvailability
    {
        /// <summary>Only in the editor, never in builds.</summary>
        EditorOnly,

        /// <summary>In the editor and in Development builds.</summary>
        DevelopmentBuilds,

        /// <summary>In the editor and in every build.</summary>
        AllBuilds
    }
}
