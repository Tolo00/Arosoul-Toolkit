using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// Represents a ScriptableObject-based event channel used to send
    /// and receive game-wide events without requiring direct object references.
    /// 
    /// This class enables decoupled communication between systems —
    /// for example, gameplay code can raise a GameEvent (e.g., “OnEnemyDefeated”),
    /// and multiple listeners (UI, audio, scoring) can respond independently.
    /// 
    /// Inspector can be used to trace what objects are listenering and debug trigger logs. 
    /// 
    /// Use this to create reusable, project-wide event assets for flexible,
    /// modular, and inspector-friendly event-driven design.
    /// </summary>
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Context Framework/Game Event")]
    public class GameEvent : ScriptableObject, IEnterPlayModeCallback, IExitPlayModeCallback {
        readonly List<GameEventDelegate> _listeners = new();
        public List<GameEventDelegate> GetListeners() => _listeners;

        public void RegisterListener(GameEventDelegate listener) {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventDelegate listener) {
            _listeners.Remove(listener);
        }

        public void Raise(UnityEngine.Object sender) {
            for (int i = _listeners.Count-1; i >= 0; i--) {
                if (_listeners[i] == null) { 
                    _listeners.RemoveAt(i);
                    continue;
                }

                _listeners[i]?.Invoke(sender);
            }
            #if UNITY_EDITOR
                RegisterRaiseForDebugger(sender);
            #endif
        }


        // Editor state - DEBUG MODE
        [NonSerialized]
        private bool _debugMode = false;
        public bool DebugMode => _debugMode;
        public void SetDebugMode(bool enabled) => _debugMode = enabled;

        public struct EventRaisedLog {
            public UnityEngine.Object Sender;
            public DateTime DateTime;
        }
        private readonly List<EventRaisedLog> _raiseLogs = new();
        public EventRaisedLog[] GetRaiseLogs() => _raiseLogs.ToArray();

        private const int MAX_LOG_AMOUNT = 15;

        void OnEnable() {
            OnEnterPlayMode();
            _debugMode = false;
        }

        public void OnEnterPlayMode() {
            ClearLogs();
            _listeners.Clear();
            _debugMode = false;
        }

        public void OnExitPlayMode() {
            _debugMode = false;
        }

        private void RegisterRaiseForDebugger(UnityEngine.Object sender) {
            if (!_debugMode) return;
            _raiseLogs.Insert(0, new EventRaisedLog {
                Sender = sender,
                DateTime = DateTime.Now
            });

            // Enforce log limit
            if (_raiseLogs.Count > MAX_LOG_AMOUNT) {
                _raiseLogs.RemoveAt(MAX_LOG_AMOUNT);
            }
        }

        public void ClearLogs() => _raiseLogs.Clear();
    }

    public delegate void GameEventDelegate(UnityEngine.Object sender);
}