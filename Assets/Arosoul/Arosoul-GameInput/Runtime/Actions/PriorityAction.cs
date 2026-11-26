using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arosoul.GameInput {

    /// <summary>
    /// Only the listener(s) with the highest priority get the callback.
    /// Typical use: A Back / Cancel action where the top-most open menu should handle it. Actions whose name ends in "Priority" become this type.
    /// </summary>
    public sealed class PriorityAction : GameAction {

        /// <summary>Token returned by Register, used to unregister or change priority.</summary>
        public sealed class Registration {
            internal readonly Entry Entry;
            internal Registration(Entry entry) => Entry = entry;

            /// <summary>Higher value = higher priority.</summary>
            public void SetPriority(int priority) => Entry.Priority = priority;
        }

        internal sealed class Entry {
            public Action<InputAction.CallbackContext> Callback;
            public int Priority;
        }

        readonly List<Entry> entries = new List<Entry>();
        readonly List<Entry> invokeBuffer = new List<Entry>(); // callbacks may (un)register while being invoked

        public PriorityAction(string mapName, string actionName) : base(mapName, actionName) { }

        public Registration Register(Action<InputAction.CallbackContext> callback, int priority) {
            var entry = new Entry { Callback = callback, Priority = priority };
            entries.Add(entry);
            return new Registration(entry);
        }

        public void Unregister(Registration registration) {
            if (registration == null || !entries.Remove(registration.Entry)) {
                Debug.LogError($"[GameInput] Unable to find registered listener for '{Path}'.");
            }
        }

        internal override void Invoke(InputAction.CallbackContext ctx) {
            if (entries.Count == 0) return;

            int highest = int.MinValue;
            foreach (var e in entries) if (e.Priority > highest) highest = e.Priority;

            invokeBuffer.Clear();
            foreach (var e in entries) if (e.Priority == highest) invokeBuffer.Add(e);

            if (invokeBuffer.Count > 1) {
                Debug.LogWarning($"[GameInput] {invokeBuffer.Count} listeners of '{Path}' share the highest priority ({highest}). Invoking all of them.");
            }

            foreach (var e in invokeBuffer) e.Callback?.Invoke(ctx);
            invokeBuffer.Clear();
            Validator.LogVerbose($"Priority action '{Path}' raised.");
        }

        internal override void ResetFrameState() { }
    }

}
