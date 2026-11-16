using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    /// <summary>Brand specific names for gamepad controls, so prompts say "Cross" on a DualSense and "A" on an Xbox pad.</summary>
    internal static class GamepadGlyphs {
        // Order: Xbox, PlayStation, Switch.
        static readonly Dictionary<string, string[]> names = new Dictionary<string, string[]> {
            ["buttonSouth"] = new[] { "A", "Cross", "B" },
            ["buttonEast"] = new[] { "B", "Circle", "A" },
            ["buttonWest"] = new[] { "X", "Square", "Y" },
            ["buttonNorth"] = new[] { "Y", "Triangle", "X" },
            ["leftShoulder"] = new[] { "LB", "L1", "L" },
            ["rightShoulder"] = new[] { "RB", "R1", "R" },
            ["leftTrigger"] = new[] { "LT", "L2", "ZL" },
            ["rightTrigger"] = new[] { "RT", "R2", "ZR" },
            ["leftStickPress"] = new[] { "LS Click", "L3", "LS Click" },
            ["rightStickPress"] = new[] { "RS Click", "R3", "RS Click" },
            ["start"] = new[] { "Menu", "Options", "+" },
            ["select"] = new[] { "View", "Share", "-" },
            ["dpad"] = new[] { "D-Pad", "D-Pad", "D-Pad" },
            ["leftStick"] = new[] { "Left Stick", "Left Stick", "Left Stick" },
            ["rightStick"] = new[] { "Right Stick", "Right Stick", "Right Stick" },
        };

        static readonly string[] gamepadDevices = { "<Gamepad>", "<XInputController>", "<DualShockGamepad>", "<SwitchProControllerHID>" };

        public static string Format(string path, GameInput.GamepadKind kind) {
            int slash = path.IndexOf('/');
            if (slash > 0 && System.Array.IndexOf(gamepadDevices, path.Substring(0, slash)) >= 0) {
                string control = path.Substring(slash + 1);
                int sub = control.IndexOf('/');
                string head = sub < 0 ? control : control.Substring(0, sub);

                if (names.TryGetValue(head, out var set)) {
                    int index = kind == GameInput.GamepadKind.PlayStation ? 1 : kind == GameInput.GamepadKind.Switch ? 2 : 0;
                    return sub < 0 ? set[index] : set[index] + " " + NameUtil.Nicify(control.Substring(sub + 1));
                }
            }

            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

}
