using System;
using UnityEngine.InputSystem;

namespace GameInputSystem
{
    public enum ButtonContext { Pressed, Released }
    public class ButtonAction : GameAction {
        public bool IsPressed { get; internal set; }

        private bool _wasPressedThisFrame;
        private bool _wasReleasedThisFrame;

        /// <summary>
        /// Returns true the frame the button is pressed down.
        /// </summary>
        public bool WasPressedThisFrame
        {
            get
            {
                Validator.ValidateNotInFixedUpdate("Button Action 'WasPressedThisFrame'");
                return _wasPressedThisFrame;
            }
            private set
            {
                _wasPressedThisFrame = value;
            }
        }

        /// <summary>
        /// Returns true the frame the button is released.
        /// </summary>
        public bool WasReleasedThisFrame
        {
            get
            {
                Validator.ValidateNotInFixedUpdate("Button Action 'WasReleasedThisFrame'");
                return _wasReleasedThisFrame;
            }
            private set
            {
                _wasReleasedThisFrame = value;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            if (ctx.performed)
            {
                IsPressed = true;
                WasPressedThisFrame = true;
                Validator.LogVerbose($"Button Action '{ctx.action.name}' pressed.");
            } 
            else if (ctx.canceled)
            {
                IsPressed = false;
                WasReleasedThisFrame = true;
                Validator.LogVerbose($"Button Action '{ctx.action.name}' released.");
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            WasPressedThisFrame = false;
            WasReleasedThisFrame = false;
        }
    }
}