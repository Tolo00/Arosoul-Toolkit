using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem 
{
    public partial class GameInput : MonoBehaviour {
        private static GameInput inst;
        public static GameInput Inst {
            get {
                if (inst == null)
                {
                    inst = FindAnyObjectByType<GameInput>(FindObjectsInactive.Include);

                    if (inst == null) {
                        GameObject go = new GameObject("[GameInput]");
                        inst = go.AddComponent<GameInput>();
                        DontDestroyOnLoad(inst.gameObject);
                    }
                }

                return inst;
            }
        }

        private InputActions _inputActions;
        private List<GameAction> _activeActions = new(); // Used to reset input state at end of frame.

        private static bool _isQuitting = false;

        void OnEnable() {
            _inputActions = new InputActions();
            _inputActions.Enable();

            RegisterActions();
            Validator.LogVerbose($"{_activeActions.Count} actions registered.");
        }

        void OnDisable() {
            // Clean-up
            _inputActions.Disable();
            _inputActions.Dispose();
        }

        void LateUpdate() {
            ResetActions();
        }

        private void ResetActions() {
            foreach (GameAction action in _activeActions) {
                action.ResetFrameState();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _ = Inst;
        }

        void OnApplicationQuit()
        {
            _isQuitting = true;
        }


        #region Input Action Registration

        private void RegisterAction(InputAction inputAction, GameAction gameAction) {
            inputAction.started += ctx => ExecuteInputAction(gameAction, ctx);
            inputAction.performed += ctx => ExecuteInputAction(gameAction, ctx);
            inputAction.canceled += ctx => ExecuteInputAction(gameAction, ctx);

            _activeActions.Add(gameAction);
        }

        /// <summary>
        /// Ensure anonymous function throws exceptions in the console.
        /// Update active device.
        /// </summary>
        private void ExecuteInputAction(GameAction gameAction, InputAction.CallbackContext ctx) {
            UpdateActiveDevice(ctx);

            try {
                gameAction.Invoke(ctx);
            }
            catch {
                throw;
            }
        }

    #endregion

    #region Device Switching
        public enum DeviceType { Keyboard, Gamepad }

        // Active input device state
        public static DeviceType ActiveDevice { get; private set; } = DeviceType.Keyboard;

        public static bool IsMouseVisible { get; private set; } = true;
        public static bool WantsMouseVisible  { get; private set; } = true;

        // Event Callback
        public static event Action<DeviceType> OnActiveDeviceChanged;

        /// <summary>
        /// Sets the preferred mouse visibility.
        /// The mouse will only be shown when the active input device is Keyboard.
        /// This behavior can be overridden via GameInput_Config.cs.
        /// </summary>
        public static void SetMouseVisibilityPreference(bool visible) {
            if (_isQuitting) return;

            WantsMouseVisible = visible;
            Validator.LogVerbose($"Mouse visibility preference changed: {WantsMouseVisible}"); 
            Inst.UpdateMouseVisibility();
        }

        private void UpdateActiveDevice(InputAction.CallbackContext ctx) {
            var device = ctx.control.device;
            DeviceType deviceUsed;

            if (device is Gamepad) deviceUsed = DeviceType.Gamepad;
            else if (device is Keyboard || device is Mouse) deviceUsed = DeviceType.Keyboard;
            else {
                Debug.LogError($"Game input does not implement device used: {device.name}");
                return;
            }

            if (deviceUsed != ActiveDevice) {
                // Device switch detected
                ActiveDevice = deviceUsed;
                UpdateMouseVisibility();
                OnActiveDeviceChanged?.Invoke(deviceUsed);
                Validator.LogVerbose($"Switched active device to: {ActiveDevice}");
            }
        }

        private void UpdateMouseVisibility() {
            if (WantsMouseVisible && (ActiveDevice == DeviceType.Keyboard || !HideMouseOnGamepadUse)) {
                IsMouseVisible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else {
                IsMouseVisible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            Validator.LogVerbose($"Mouse visibility changed: {IsMouseVisible}");
        }

    #endregion

    }
}


    