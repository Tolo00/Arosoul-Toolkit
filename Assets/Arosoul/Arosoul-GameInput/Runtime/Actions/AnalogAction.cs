using System;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    public sealed class AnalogAction : GameAction {
        bool startedThisFrame;
        bool canceledThisFrame;

        public AnalogAction(string mapName, string actionName) : base(mapName, actionName) { }

        public float Value { get; private set; }

        public bool StartedThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Analog Action 'StartedThisFrame'");
                return startedThisFrame;
            }
        }

        public bool CanceledThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Analog Action 'CanceledThisFrame'");
                return canceledThisFrame;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            Value = ctx.ReadValue<float>();

            if (ctx.started) startedThisFrame = true;
            if (ctx.canceled) {
                canceledThisFrame = true;
                Value = 0f;
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            startedThisFrame = false;
            canceledThisFrame = false;
        }

        internal override void ResetState() {
            base.ResetState();
            Value = 0f;
        }
    }

}
