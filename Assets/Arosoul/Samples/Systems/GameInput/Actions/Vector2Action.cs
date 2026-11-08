using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem
{
    public class Vector2Action : GameAction {
        public Vector2 Value;
        public float X => Value.x;
        public float Y => Value.y;
        
        private bool _startedThisFrame;
        private bool _canceledThisFrame;

        /// <summary>
        /// Returns true the frame this action is started.
        /// </summary>
        public bool StartedThisFrame 
        { 
            get 
            {
                Validator.ValidateNotInFixedUpdate("Vector2 Action 'StartedThisFrame'");
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
                Validator.ValidateNotInFixedUpdate("Vector2 Action 'CanceledThisFrame'");
                return _canceledThisFrame;
            } 
            private set 
            {
                _canceledThisFrame = value;
            }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        internal override void Invoke(InputAction.CallbackContext ctx) {
            Value = ctx.ReadValue<Vector2>();

            if (ctx.started) 
            {
                StartedThisFrame = true;
                Validator.LogVerbose($"Vector2 Action '{ctx.action.name}' started.");
            }
            if (ctx.canceled) {
                CanceledThisFrame = true;
                Value = Vector2.zero;
                Validator.LogVerbose($"Vector2 Action '{ctx.action.name}' canceled.");
            }

            OnEvent?.Invoke(ctx);
        }

        internal override void ResetFrameState() {
            StartedThisFrame = false;
            CanceledThisFrame = false;
        }
    }
}