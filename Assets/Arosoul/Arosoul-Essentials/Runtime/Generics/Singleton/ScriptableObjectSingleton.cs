using UnityEngine;

namespace Arosoul.Essentials 
{
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst == null)
                {
                    var loaded = Resources.LoadAll<T>("");
                    Debug.Assert(loaded.Length != 0, $"{typeof(T).Name} could not be found.");
                    Debug.Assert(loaded.Length <= 1, $"Multiple objects of type {typeof(T).Name} found.");
                    _inst = loaded[0];
                }
                return _inst;
            }
        }
    }
}