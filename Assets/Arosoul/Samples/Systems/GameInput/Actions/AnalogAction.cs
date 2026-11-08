using System;
using UnityEngine.InputSystem;

namespace GameInputSystem
{
    public class AnalogAction : GameAction {
        public float Value;
        
        private bool _startedThisFrame;
        private bool _canceledThisFrame;

        /// <summary>
        /// Returns true the frame this action is started.
        /// </summary>
        public bool StartedThisFrame 
        { 
            get 
            {
                Validator.ValidateNotInFixedUpdate("Analog Action 'StartedThisFrame'");
                return _startedThisFrame;
            } 
            private set 
            {
                _startedThisFrame = value;
            }
        }

        /// <summary>
        /// Returns true the frame this action is canceled.
        /// </summary>
        public bool CanceledThisFrame
        {
            get 
            {
                Validator.ValidateNotInFixedUpdate("Analog Action 'CanceledThisFrame'");
                return _canceledThisFrame;
            } 
            private set 
            {
                _canceledThisFrame = value;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            Value = ctx.ReadValue<float>();

            if (ctx.started) 
            {
                StartedThisFrame = true;
                Validator.LogVerbose($"Analog Action '{ctx.action.name}' started.");
            }
            if (ctx.canceled) {
                CanceledThisFrame = true;
                Value = 0f;
                Validator.LogVerbose($"Analog Action '{ctx.action.name}' canceled.");
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            StartedThisFrame = false;
            CanceledThisFrame = false;
        }
    }
}