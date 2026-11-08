using System;
using UnityEngine;

namespace Arosoul.Essentials.StateMachine {
    public abstract class State<EState, TContext> 
        where EState : Enum
        where TContext : StateContext 
    {
        protected TContext Context;
        
        private float _enterTime = 0f;
        protected float TimeInState => Time.time - _enterTime;

        internal EState EnumValue;
        internal StateMachine<EState, TContext> Machine;
        internal object MachineOwner;

        public State(TContext context) {
            Context = context;
        }

        // Internal lifecycle hooks
        internal void Enter() {
            _enterTime = Time.time;
            OnEnter();
        }

        internal void Exit() {
            OnExit();
        }

        internal void Update() {
            OnUpdate();
        }

        internal void FixedUpdate() {
            OnFixedUpdate();
        }

        internal void EveryUpdate() {
            OnEveryUpdate();
        } 

        // Overridable state logic

        /// <summary>
        /// Try get the next state. 
        /// Return true to switch to selected, false to remain in current state.
        /// </summary>
        internal abstract bool TryGetNextState(out EState newState);
        
        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnFixedUpdate() { }
        protected virtual void OnEveryUpdate() { } // Called every update, even when not the active state.
    }
}
