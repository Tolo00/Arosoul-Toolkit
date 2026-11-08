using System;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// A generic ScriptableObject that stores and manages a runtime variable value.
    /// 
    /// Provides a persistent data container that can be shared across scenes or objects,
    /// allowing centralized control of game state without direct references.
    /// 
    /// The variable can be initialized with a starting value and optionally be marked as read-only.
    /// 
    /// Commonly used for global data such as health, score, or configuration values
    /// in event-driven or data-oriented Unity architectures.
    /// </summary>
    public abstract class VariableBase<T> : ScriptableObject, IEnterPlayModeCallback, IExitPlayModeCallback {

    #if UNITY_EDITOR
        [TextArea]
        public string _developerDescription = "";
    #endif

        [SerializeField] private T _runtimeValue;
        [SerializeField] private T _initialValue;

        public T Value {
            get { return _runtimeValue; }
            set {
                if (_readOnly) { 
                    Debug.LogWarning("Cannot set value of readonly variable.");
                    return;
                }

                _runtimeValue = value;
                OnValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public T InitialValue {
            get { return _initialValue; }
            set { _initialValue = value; }
        }


        [SerializeField] private bool _readOnly;
        [SerializeField] private bool _applyOnExit;

        public event EventHandler OnValueChanged;



        void OnEnable() {
            _runtimeValue = _initialValue;
        }


        public void OnEnterPlayMode() {
            _runtimeValue = _initialValue;
        }

        public void OnExitPlayMode() {
            if (!_applyOnExit) return;

            _initialValue = _runtimeValue;
            _applyOnExit = false;

        #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
        #endif
        }
    }

}