using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arosoul.GameInput {

    public sealed class Vector2Action : GameAction {
        bool startedThisFrame;
        bool canceledThisFrame;

        public Vector2Action(string mapName, string actionName) : base(mapName, actionName) { }

        public Vector2 Value { get; private set; }
        public float X => Value.x;
        public float Y => Value.y;

        public bool StartedThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Vector2 Action 'StartedThisFrame'");
                return startedThisFrame;
            }
        }

        public bool CanceledThisFrame {
            get {
                Validator.ValidateNotInFixedUpdate("Vector2 Action 'CanceledThisFrame'");
                return canceledThisFrame;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            Value = ctx.ReadValue<Vector2>();

            if (ctx.started) startedThisFrame = true;
            if (ctx.canceled) {
                canceledThisFrame = true;
                Value = Vector2.zero;
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            startedThisFrame = false;
            canceledThisFrame = false;
        }

        internal override void ResetState() {
            base.ResetState();
            Value = Vector2.zero;
        }
    }

}
