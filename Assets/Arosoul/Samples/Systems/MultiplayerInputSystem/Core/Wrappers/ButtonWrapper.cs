using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public class ButtonWrapper : IInputActionWrapper {
        public bool IsPressed { get; internal set; }

        private bool _wasPressedThisFrame;
        private bool _wasReleasedThisFrame;
        private bool _wasPerformedThisFrame;

        /// <summary>
        /// Returns true the frame the button is pressed down.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool WasPressedThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Button", "WasPressedThisFrame");
                return _wasPressedThisFrame;
            } 
            internal set { _wasPressedThisFrame = value; }
        }

        /// <summary>
        /// Returns true the frame the button is released.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool WasReleasedThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Button", "WasReleasedThisFrame");
                return _wasReleasedThisFrame;
            } 
            internal set { _wasReleasedThisFrame = value; }
        }

        /// <summary>
        /// Returns true the frame the button was "preformed".
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool WasPerformedThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Button", "WasPerformedThisFrame");
                return _wasPerformedThisFrame;
            } 
            internal set { _wasPerformedThisFrame = value; }
        }

        public event Action<InputAction.CallbackContext> OnEvent;


        public void UpdateState(InputAction.CallbackContext context) {
            if (context.started) {
                IsPressed = true;
                WasPressedThisFrame = true;
            } else if (context.canceled) {
                IsPressed = false;
                WasReleasedThisFrame = true;
            }

            if (context.performed) {
                WasPerformedThisFrame = true;
            }

            OnEvent?.Invoke(context);
        }

        public void ResetStateEndOfFrame() {
            WasPressedThisFrame = false;
            WasReleasedThisFrame = false;
            WasPerformedThisFrame = false;
        }

        public InputAction InputAction { get; set; }
    }
}