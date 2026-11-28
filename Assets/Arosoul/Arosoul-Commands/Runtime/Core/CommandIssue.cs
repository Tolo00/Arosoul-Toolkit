using System.Reflection;

namespace Arosoul.Commands
{
    public enum CommandIssueSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// A problem found while discovering commands, shown in the Commands Debugger and by the agent tools.
    /// Members with errors are not registered as commands.
    /// </summary>
    public sealed class CommandIssue
    {
        public CommandIssue(CommandIssueSeverity severity, string message, MemberInfo member)
        {
            Severity = severity;
            Message = message;
            Member = member;
        }

        public CommandIssueSeverity Severity { get; }
        public string Message { get; }

        /// <summary>
        /// The member the issue is about, or null when it concerns several members.
        /// </summary>
        public MemberInfo Member { get; }

        public override string ToString() => $"{(Severity == CommandIssueSeverity.Error ? "ERROR" : "WARNING")}: {Message}";
    }
}
