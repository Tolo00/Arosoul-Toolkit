using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// The lines shown in the console: typed commands, command output and mirrored Unity log messages.
    /// </summary>
    public sealed class CommandLog
    {
        private readonly List<CommandLogEntry> _entries = new();
        private int _maxEntries = 500;

        /// <summary>
        /// The log shown by the in-game console.
        /// </summary>
        public static CommandLog Default { get; } = new();

        public IReadOnlyList<CommandLogEntry> Entries => _entries;

        /// <summary>
        /// Oldest entries are removed beyond this count.
        /// </summary>
        public int MaxEntries
        {
            get => _maxEntries;
            set
            {
                _maxEntries = Mathf.Max(1, value);
                TrimToMax();
            }
        }

        public event Action<CommandLogEntry> OnEntryAdded;
        public event Action OnCleared;

        public void Add(string message, CommandLogType type = CommandLogType.Output, string stackTrace = null)
        {
            var entry = new CommandLogEntry(message, type, stackTrace);
            _entries.Add(entry);
            TrimToMax();
            OnEntryAdded?.Invoke(entry);
        }

        public void Clear()
        {
            _entries.Clear();
            OnCleared?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Default._entries.Clear();
        }

        private void TrimToMax()
        {
            if (_entries.Count > _maxEntries)
                _entries.RemoveRange(0, _entries.Count - _maxEntries);
        }
    }
}
