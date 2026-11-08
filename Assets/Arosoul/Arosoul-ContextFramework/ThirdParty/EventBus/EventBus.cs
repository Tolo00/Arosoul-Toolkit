using System.Collections.Generic;
using UnityEngine;

/// This is an implementation of the Event Bus Pattern designed by git-amend on youtube.
/// Resource: https://www.youtube.com/watch?v=4_DTAnigmaQ&t=72s&ab_channel=git-amend

namespace Arosoul.ContextFramework.ThirdParty {

    public static class EventBus<T> where T : IEvent {
        static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();

        public static void Register(EventBinding<T> binding) => bindings.Add(binding);
        public static void Deregister(EventBinding<T> binding) => bindings.Remove(binding);

        public static void Raise(T @event) {
            foreach (var binding in bindings) {
                binding.OnEvent.Invoke(@event);
                binding.OnEventNoArgs.Invoke();
            }
        }

        static void Clear() {
            if (EventBusUtil.ShowDebugLogs) Debug.Log($"Clearing {typeof(T).Name} bindings");
            bindings.Clear();
        }
    }

}