using System.Collections.Generic;

namespace Arosoul.Scenes.Editor
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct ValidationIssue
    {
        public readonly ValidationSeverity Severity;
        public readonly string Message;

        public ValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public override string ToString() => $"[{Severity}] {Message}";
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();

        public IReadOnlyList<ValidationIssue> Issues => _issues;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public void Error(string message)
        {
            _issues.Add(new ValidationIssue(ValidationSeverity.Error, message));
            ErrorCount++;
        }

        public void Warning(string message)
        {
            _issues.Add(new ValidationIssue(ValidationSeverity.Warning, message));
            WarningCount++;
        }

        public void Info(string message)
        {
            _issues.Add(new ValidationIssue(ValidationSeverity.Info, message));
        }

        public bool Contains(ValidationSeverity severity, string messagePart)
        {
            foreach (var issue in _issues)
                if (issue.Severity == severity && issue.Message.Contains(messagePart)) return true;
            return false;
        }
    }
}
