using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

namespace Arosoul.GameInput {

    /// <summary>
    /// Device state and utilities. Actions are accessed through the generated GameActions class, remapping through GameInputBindings.
    /// </summary>
    public static class GameInput {
        public enum DeviceType { Keyboard, Gamepad }
        public enum GamepadKind { Xbox, PlayStation, Switch, Generic }

#region Active device

        /// <summary>The device the player used last. Keyboard covers keyboard and mouse.</summary>
        public static DeviceType ActiveDevice { get; private set; } = DeviceType.Keyboard;

        /// <summary>Brand of the gamepad used last. Use it to pick button prompt icons.</summary>
        public static GamepadKind ActiveGamepadKind { get; private set; } = GamepadKind.Xbox;

        public static Gamepad ActiveGamepad { get; private set; }

        public static bool IsInitialized => GameInputRuntime.IsRunning;

        public static event Action<DeviceType> OnActiveDeviceChanged;
        public static event Action<GamepadKind> OnGamepadKindChanged;

#endregion

#region Mouse

        public static bool IsMouseVisible { get; private set; } = true;
        public static bool WantsMouseVisible { get; private set; } = true;

        /// <summary>
        /// Sets the preferred mouse visibility. The mouse is only shown while the active device is the keyboard,
        /// unless "Hide Mouse On Gamepad Use" is turned off in the config.
        /// </summary>
        public static void SetMouseVisibilityPreference(bool visible) {
            WantsMouseVisible = visible;
            Validator.LogVerbose($"Mouse visibility preference changed: {visible}");
            UpdateMouseVisibility();
        }

        static void UpdateMouseVisibility() {
            bool hideOnGamepad = GameInputRuntime.Config == null || GameInputRuntime.Config.HideMouseOnGamepadUse;
            bool visible = WantsMouseVisible && (ActiveDevice == DeviceType.Keyboard || !hideOnGamepad);

            IsMouseVisible = visible;
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Validator.LogVerbose($"Mouse visibility changed: {visible}");
        }

#endregion

#region Action Maps

        public static void SetMapEnabled(string mapName, bool enabled) {
            var map = GameInputRuntime.Asset != null ? GameInputRuntime.Asset.FindActionMap(mapName, false) : null;
            if (map == null) { Debug.LogError($"[GameInput] Action map '{mapName}' not found."); return; }

            if (enabled) map.Enable();
            else map.Disable();
        }

        public static bool IsMapEnabled(string mapName) {
            var map = GameInputRuntime.Asset != null ? GameInputRuntime.Asset.FindActionMap(mapName, false) : null;
            return map != null && map.enabled;
        }

#endregion

#region Rumble

        /// <summary>Turn rumble off for players who don't want it. Initial value comes from the config.</summary>
        public static bool RumbleEnabled { get; set; } = true;

        static float rumbleEndTime = -1f;

        /// <summary>Vibrates the active gamepad (low = heavy motor, high = light motor, both 0-1).</summary>
        public static void Rumble(float low, float high, float duration) {
            var pad = ActiveGamepad ?? Gamepad.current;
            if (!RumbleEnabled || pad == null) return;

            pad.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
            rumbleEndTime = Time.unscaledTime + duration;
        }

        public static void StopRumble() {
            rumbleEndTime = -1f;
            var pad = ActiveGamepad ?? Gamepad.current;
            pad?.ResetHaptics();
        }

        internal static void TickRumble() {
            if (rumbleEndTime >= 0f && Time.unscaledTime >= rumbleEndTime) StopRumble();
        }

#endregion

#region Internal: Called by GameInputRuntime

        internal static void ReportDevice(InputDevice device) {
            DeviceType type;
            if (device is Gamepad) type = DeviceType.Gamepad;
            else if (device is Keyboard || device is Mouse) type = DeviceType.Keyboard;
            else return; // touch, joysticks... don't change the active device

            if (type == DeviceType.Gamepad) {
                ActiveGamepad = (Gamepad)device;
                var kind = DetectKind(device);
                if (kind != ActiveGamepadKind) {
                    ActiveGamepadKind = kind;
                    OnGamepadKindChanged?.Invoke(kind);
                }
            }

            if (type == ActiveDevice) return;

            ActiveDevice = type;
            if (type == DeviceType.Keyboard) StopRumble();
            UpdateMouseVisibility();
            OnActiveDeviceChanged?.Invoke(type);
            Validator.LogVerbose($"Switched active device to: {type}");
        }

        internal static void ReportDeviceLost(InputDevice device) {
            if (device != ActiveGamepad) return;
            ActiveGamepad = null;

            // Fall back to another connected gamepad, otherwise to keyboard and mouse.
            foreach (var pad in Gamepad.all) {
                if (pad != device) { ReportDevice(pad); return; }
            }
            ReportDevice(Keyboard.current ?? (InputDevice)Mouse.current);
        }

        internal static void ResetState() {
            ActiveDevice = DeviceType.Keyboard;
            ActiveGamepadKind = GamepadKind.Xbox;
            ActiveGamepad = null;
            IsMouseVisible = true;
            WantsMouseVisible = true;
            rumbleEndTime = -1f;
        }

        static GamepadKind DetectKind(InputDevice device) {
            if (device is DualShockGamepad) return GamepadKind.PlayStation;
            if (device is XInputController) return GamepadKind.Xbox;
            if (device is SwitchProControllerHID) return GamepadKind.Switch;

            string product = (device.description.product ?? string.Empty).ToLowerInvariant();
            if (product.Contains("dualsense") || product.Contains("dualshock") || product.Contains("playstation")) return GamepadKind.PlayStation;
            if (product.Contains("xbox")) return GamepadKind.Xbox;
            if (product.Contains("nintendo") || product.Contains("switch")) return GamepadKind.Switch;
            return GamepadKind.Generic;
        }
    
#endregion
    
    }
}
