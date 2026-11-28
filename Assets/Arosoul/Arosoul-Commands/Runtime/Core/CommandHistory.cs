using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// Previously submitted commands, browsed newest first. Optionally stored in PlayerPrefs between sessions.
    /// </summary>
    public sealed class CommandHistory
    {
        private readonly List<string> _entries = new();
        private readonly string _prefsKey;
        private int _cursor;
        private string _draft = string.Empty;

        /// <param name="capacity">Maximum number of commands kept.</param>
        /// <param name="prefsKey">PlayerPrefs key to load and save the history with, or null to keep it in memory only.</param>
        public CommandHistory(int capacity, string prefsKey = null)
        {
            Capacity = Mathf.Max(0, capacity);
            _prefsKey = prefsKey;
            Load();
            _cursor = _entries.Count;
        }

        public int Capacity { get; }

        /// <summary>
        /// Oldest first.
        /// </summary>
        public IReadOnlyList<string> Entries => _entries;

        public bool IsBrowsing => _cursor < _entries.Count;

        public void Add(string command)
        {
            ResetNavigation();
            if (Capacity == 0 || string.IsNullOrWhiteSpace(command)) return;

            command = command.Trim();
            _entries.Remove(command);
            _entries.Add(command);
            if (_entries.Count > Capacity) _entries.RemoveRange(0, _entries.Count - Capacity);

            _cursor = _entries.Count;
            Save();
        }

        /// <summary>
        /// Step to an older command.
        /// </summary>
        /// <param name="currentInput">The text in the input field, restored when stepping past the newest command.</param>
        public bool TryGetPrevious(string currentInput, out string command)
        {
            command = null;
            if (_cursor == 0 || _entries.Count == 0) return false;

            if (!IsBrowsing) _draft = currentInput ?? string.Empty;
            _cursor--;
            command = _entries[_cursor];
            return true;
        }

        /// <summary>
        /// Step to a newer command. Stepping past the newest returns the text that was typed before browsing.
        /// </summary>
        public bool TryGetNext(out string command)
        {
            command = null;
            if (!IsBrowsing) return false;

            _cursor++;
            command = IsBrowsing ? _entries[_cursor] : _draft;
            return true;
        }

        public void ResetNavigation()
        {
            _cursor = _entries.Count;
            _draft = string.Empty;
        }

        public void Clear()
        {
            _entries.Clear();
            ResetNavigation();
            Save();
        }

        private void Load()
        {
            if (string.IsNullOrEmpty(_prefsKey) || Capacity == 0) return;

            string stored = PlayerPrefs.GetString(_prefsKey, string.Empty);
            foreach (string line in stored.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                _entries.Add(line);

            if (_entries.Count > Capacity) _entries.RemoveRange(0, _entries.Count - Capacity);
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_prefsKey)) return;

            PlayerPrefs.SetString(_prefsKey, string.Join("\n", _entries));
        }
    }
}
