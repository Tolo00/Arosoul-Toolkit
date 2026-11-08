using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public class AnalogWrapper : IInputActionWrapper {
        public float Value { get; internal set; }

        private bool _wasStartedThisFrame;
        private bool _wasCanceledThisFrame;

        /// <summary>
        /// Returns true the frame the float input is pressed down.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool WasStartedThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Button", "WasStartedThisFrame");
                return _wasStartedThisFrame;
            } 
            internal set { _wasStartedThisFrame = value; }
        }

        /// <summary>
        /// Returns true the frame the float input is released.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool WasCanceledThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Button", "WasCanceledThisFrame");
                return _wasCanceledThisFrame;
            } 
            internal set { _wasCanceledThisFrame = value; }
        }

        public event Action<InputAction.CallbackContext> OnEvent;


        public void UpdateState(InputAction.CallbackContext context) {
            if (context.started) {
                WasStartedThisFrame = true;
            } else if (context.canceled) {
                WasCanceledThisFrame = true;
                Value = 0;
            }

            Value = context.ReadValue<float>();
            OnEvent?.Invoke(context);
        }

        public void ResetStateEndOfFrame() {
            WasStartedThisFrame = false;
            WasCanceledThisFrame = false;
        }

        public InputAction InputAction { get; set; }
    }
}