using System;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    public sealed class ButtonAction : GameAction {
        bool wasPressedThisFrame;
        bool wasReleasedThisFrame;

        public ButtonAction(string mapName, string actionName) : base(mapName, actionName) { }

        public bool IsPressed { get; private set; }

        /// <summary>True during the frame the button went down.</summary>
        public bool WasPressedThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Button Action 'WasPressedThisFrame'");
                return wasPressedThisFrame;
            }
        }

        /// <summary>True during the frame the button was released.</summary>
        public bool WasReleasedThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Button Action 'WasReleasedThisFrame'");
                return wasReleasedThisFrame;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            if (ctx.performed) {
                IsPressed = true;
                wasPressedThisFrame = true;
                Validator.LogVerbose($"Button '{Path}' pressed.");
            }
            else if (ctx.canceled) {
                IsPressed = false;
                wasReleasedThisFrame = true;
                Validator.LogVerbose($"Button '{Path}' released.");
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            wasPressedThisFrame = false;
            wasReleasedThisFrame = false;
        }

        internal override void ResetState() {
            base.ResetState();
            IsPressed = false;
        }
    }

}
