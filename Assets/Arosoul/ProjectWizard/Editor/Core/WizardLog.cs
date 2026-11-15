using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectWizard
{
    public enum LogLevel { Info, Success, Warning, Error }

    /// <summary>
    /// Log shown in the wizard window. Stored in SessionState so it survives the domain reloads
    /// that package installs cause.
    /// </summary>
    public static class WizardLog
    {
        private const string Key = "ProjectWizard.Log";
        private const int MaxLines = 400;

        public static event Action Changed;

        /// <summary>Increases whenever the log changes. Lets the UI know when to redraw.</summary>
        public static int Revision { get; private set; }

        public static void Info(string message)    => Add(LogLevel.Info, message);
        public static void Success(string message) => Add(LogLevel.Success, message);
        public static void Warn(string message)    => Add(LogLevel.Warning, message);
        public static void Error(string message)   => Add(LogLevel.Error, message);

        public static void Add(LogLevel level, string message)
        {
            var lines = Load();
            lines.Add(new LogLine { level = (int)level, text = message });
            if (lines.Count > MaxLines) lines.RemoveRange(0, lines.Count - MaxLines);
            Save(lines);

            var plain = "[Project Wizard] " + message;
            if (level == LogLevel.Error) Debug.LogError(plain);
            else if (level == LogLevel.Warning) Debug.LogWarning(plain);
            else Debug.Log(plain);

            Revision++;
            Changed?.Invoke();
        }

        public static void Clear()
        {
            SessionState.EraseString(Key);
            Revision++;
            Changed?.Invoke();
        }

        public static List<LogLine> Lines => Load();

        // ── storage ──────────────────────────────────────────────────────

        [Serializable] public class LogLine { public int level; public string text; }
        [Serializable] private class LogFile { public List<LogLine> lines = new List<LogLine>(); }

        private static List<LogLine> Load()
        {
            var json = SessionState.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new List<LogLine>();
            try { return JsonUtility.FromJson<LogFile>(json).lines ?? new List<LogLine>(); }
            catch { return new List<LogLine>(); }
        }

        private static void Save(List<LogLine> lines) =>
            SessionState.SetString(Key, JsonUtility.ToJson(new LogFile { lines = lines }));
    }
}
