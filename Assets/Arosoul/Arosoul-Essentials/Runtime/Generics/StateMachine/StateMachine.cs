using UnityEngine;
using System;
using System.Collections.Generic;

namespace Arosoul.Essentials.StateMachine {
    public class StateContext { } // Extend with data you want available to all states

    public class StateMachine<EState, Context> 
            where EState : Enum 
            where Context : StateContext 
    {
        private readonly Dictionary<EState, State<EState, Context>> _states = new();
        private State<EState, Context> _currentState;
        private State<EState, Context> _defaultState;

        private object _owner;
        private bool _returnToDefaultTrigger;

        private bool _enabled = true;
        public bool Enabled => _enabled;
        public void Enable(bool transitionToDefault = false) {
            _enabled = true;
            if (transitionToDefault) TransitionToDefaultState();
            OnSetEnabled?.Invoke(true);
        }
        public void Disable(bool transitionToDefault = false) {
            _enabled = false;
            if (transitionToDefault) TransitionToDefaultState();
            OnSetEnabled?.Invoke(false);
        }

        public event Action<EState> OnStateChanged;
        public event Action<bool> OnSetEnabled;

        // "Last states" are used in loop protection to debug the source
        private const int LAST_STATES_COUNT = 4;
        private readonly List<EState> _lastStates = new();

#region Initialization
        public StateMachine(Dictionary<EState, State<EState, Context>> states, EState defaultState, object owner) {
            _states = states;
            _owner = owner;

            BindValuesInStates();
            SetDefaultState(defaultState);
        }

        private void SetDefaultState(EState key) {
            if (_states.TryGetValue(key, out var state)) {
                _currentState = state;
                _defaultState = state;
                state.Enter();
                OnStateChanged?.Invoke(key);
            } else {
                Debug.LogError($"Default state {key} not registered!");
            }
        }

        private void BindValuesInStates() {
            foreach (var kvp in _states) {
                kvp.Value.EnumValue = kvp.Key;
                kvp.Value.Machine = this;
                kvp.Value.MachineOwner = _owner;
            }
        }
#endregion

        /// <summary>
        /// Switch to a different state (replaces current).
        /// </summary>
        private void TransitionToState(EState key) {
            if (_states.TryGetValue(key, out var newState)) {

                _currentState.Exit();
                _currentState = newState;
                _currentState.Enter();

                OnStateChanged?.Invoke(key);
            } else {
                Debug.LogError($"State {key} not registered!");
            }
        }

        /// <summary>
        /// Transition to the default state.
        /// </summary>
        public void TransitionToDefaultState() {
            _returnToDefaultTrigger = true;
        }

    #region Update Cycle
        public void Update() {
            if (!Enabled) return;

            // Check if TransitionToDefaultState() has been called
            if (_returnToDefaultTrigger) {
                TransitionToState(_defaultState.EnumValue);
                _returnToDefaultTrigger = false;
            }

            // Check if state wants to transition
            int depth = 0;
            while (_currentState.TryGetNextState(out EState newState)) { 
                TransitionToState(newState);
                
                // Add to state log
                _lastStates.Add(newState);
                if (_lastStates.Count > LAST_STATES_COUNT) {
                    _lastStates.RemoveAt(0);
                }
                
                // Infinite loop protection
                depth++;
                if (depth > 100) {
                    Debug.LogError($"State transition loop detected '{typeof(EState).Name}'. Exceeded 100 transitions in a single update. \nLast attempted states: {_lastStates[0]}, {_lastStates[1]}, {_lastStates[2]}, {_lastStates[3]}");  
                    break;
                }
            }

            _currentState.Update();

            // Invoke OnEveryUpdate() for each state
            foreach (var kvp in _states) {
                kvp.Value.EveryUpdate();
            }
        }

        public void FixedUpdate() {
            if (!Enabled) return;

            _currentState.FixedUpdate();
        }
    #endregion

        /// <summary>
        /// Returns the key of the current state (if found).
        /// </summary>
        public EState GetCurrentStateKey() {
            foreach (var state in _states) {
                if (state.Value == _currentState) {
                    return state.Key;
                }
            }

            throw new InvalidOperationException("Current state not registered!");
        }

        
    }
}
