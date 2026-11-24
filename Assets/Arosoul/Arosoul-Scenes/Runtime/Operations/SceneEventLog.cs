using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Ring buffer of the latest Scenes messages. Shown in the scene debugger.
    /// </summary>
    public static class SceneEventLog
    {
        public readonly struct Entry
        {
            public readonly float Time;
            public readonly string Message;

            public Entry(float time, string message)
            {
                Time = time;
                Message = message;
            }
        }

        private const int Capacity = 100;
        private static readonly List<Entry> _entries = new List<Entry>();

        public static IReadOnlyList<Entry> Entries => _entries;

        public static void Add(string message)
        {
            if (_entries.Count >= Capacity) _entries.RemoveAt(0);
            _entries.Add(new Entry(Time.realtimeSinceStartup, message));
        }

        public static void Clear()
        {
            _entries.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _entries.Clear();
        }
    }
}
