using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem
{
    /// <summary>
    /// Action type that only returns an callback for the listener with highest priority.
    /// Store the returned Registration token to unregister or change priority later.
    /// </summary>
    public class PriorityAction : GameAction
    {
        /// <summary>
        /// Registration token used to unregister or change priority of a callback.
        /// </summary>
        public class Registration
        {
            internal ListenerEntry Listener { get; }
            internal Registration(ListenerEntry listener) => Listener = listener;

            /// <summary>
            /// Set priority of callback.
            /// Only registation with highest priority will have their callback invoked.
            /// </summary>
            public void SetPriority(int priority)
            {
                Listener.Priority = priority;
            }
        }

        internal class ListenerEntry
        {
            public Action<InputAction.CallbackContext> Callback;
            public int Priority;

            public ListenerEntry(Action<InputAction.CallbackContext> callback, int priority)
            {
                Callback = callback;
                Priority = priority;
            }
        }

        private readonly HashSet<ListenerEntry> _listenerEntries;

        public PriorityAction()
        {
            _listenerEntries = new HashSet<ListenerEntry>();
            
        }

        /// <summary>
        /// Registers a callback with the specified priority. Higher values = higher priority.
        /// </summary>
        /// <returns>Registration token for unregister or change priority of callback.</returns>
        public Registration Register(Action<InputAction.CallbackContext> callback, int priority)
        {
            var listener = new ListenerEntry(callback, priority);
            _listenerEntries.Add(listener);
            return new Registration(listener);
        }

        /// <summary>
        /// Unregisters a callback using its registration token.
        /// </summary>
        public void Unregister(Registration registration)
        {
            if (!_listenerEntries.Contains(registration.Listener))
            {
                Debug.LogError("Unable to find registered listener for Priority Action.");
                return;
            }

            _listenerEntries.Remove(registration.Listener);   
        }

        internal override void Invoke(InputAction.CallbackContext ctx)
        {
            if (_listenerEntries.Count == 0)
                return;

            ListenerEntry highestEntryFound = null;
            List<ListenerEntry> duplicateEntries = new List<ListenerEntry>();
            
            foreach (var currentEntry in _listenerEntries)
            {
                if (highestEntryFound == null)
                {
                    highestEntryFound = currentEntry;
                    duplicateEntries.Add(currentEntry);
                    continue;
                }
                
                if (currentEntry.Priority > highestEntryFound.Priority)
                {
                    highestEntryFound = currentEntry;
                    duplicateEntries.Clear();
                    duplicateEntries.Add(currentEntry);
                }
                else if (currentEntry.Priority == highestEntryFound.Priority)
                {
                    duplicateEntries.Add(currentEntry); // Duplicate with same priority.
                }
            }
            
            // Log warning if multiple entries with same highest priority are found.
            if (duplicateEntries.Count > 1)
            {
                Debug.LogWarning($"Multiple listener entries found with same highest priority ({highestEntryFound.Priority}) for action '{ctx.action.name}'. Invoking all of them.");
            }
            
            foreach (var entry in duplicateEntries)
            {
                entry.Callback?.Invoke(ctx);
            }
            
            Validator.LogVerbose($"Priority Action '{ctx.action.name}' raised.");
        }

        internal override void ResetFrameState() { }
    }
}