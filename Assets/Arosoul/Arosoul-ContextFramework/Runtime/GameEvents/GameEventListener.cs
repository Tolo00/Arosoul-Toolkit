using System;
using UnityEngine;
using UnityEngine.Events;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// A MonoBehaviour that listens for a specific GameEvent and triggers
    /// a UnityEvent response when the event is raised.
    /// 
    /// This allows designers to visually connect gameplay events to responses
    /// in the Unity Inspector without writing code.
    /// 
    /// Commonly used to react to global events such as UI updates,
    /// sound triggers, or scene transitions.
    /// </summary>
    public class GameEventListener : MonoBehaviour {
        public GameEvent Event;
        public UnityEvent Response;

        private GameEventDelegate _gameEvent;

        void OnEnable() {
            _gameEvent = OnEventRaised;
            Event.RegisterListener(_gameEvent);
        }

        void OnDisable() {
            Event.UnregisterListener(_gameEvent);
        }

        void OnEventRaised(UnityEngine.Object sender) {
            Response?.Invoke();
        }
    }

}