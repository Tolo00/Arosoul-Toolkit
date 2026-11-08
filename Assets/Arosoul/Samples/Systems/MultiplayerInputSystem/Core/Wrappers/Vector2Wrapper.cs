using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public class Vector2Wrapper : IInputActionWrapper {
        public Vector2 Value;
        public float X => Value.x;
        public float Y => Value.y;
        
        private bool _startedThisFrame;
        private bool _canceledThisFrame;

        /// <summary>
        /// Returns true the frame this action is started.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool StartedThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Vector2", "StartedThisFrame");
                return _startedThisFrame;
            } 
            internal set { _startedThisFrame = value; }
        }

        /// <summary>
        /// Returns true the frame this action is canceled.
        /// NOTE: Should only be accessed inside Update() callback.
        /// </summary>
        public bool CanceledThisFrame { 
            get {
                if (Time.inFixedTimeStep) InputManager.PrintWrapperFixedUpdateWarning("Vector2", "CanceledThisFrame");
                return _canceledThisFrame;
            } 
            internal set { _canceledThisFrame = value; }
        }

        public event Action<InputAction.CallbackContext> OnEvent;

        public void UpdateState(InputAction.CallbackContext context) {
            Value = context.ReadValue<Vector2>();

            if (context.started) StartedThisFrame = true;
            if (context.canceled) {
                CanceledThisFrame = true;
                Value = Vector2.zero;
            }

            OnEvent?.Invoke(context);
        }

        public void ResetStateEndOfFrame() {
            StartedThisFrame = false;
            CanceledThisFrame = false;
        }

        public InputAction InputAction { get; set; }
    }
}