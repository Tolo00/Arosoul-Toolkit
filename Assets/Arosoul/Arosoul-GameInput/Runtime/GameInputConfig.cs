using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    public enum RebindConflictMode {
        /// <summary>Two actions may share a control.</summary>
        Allow,
        /// <summary>If the new control is used by another action in the same map, that action gets the old control.</summary>
        Swap
    }

    /// <summary>
    /// The one asset GameInput needs. It must live in a Resources folder (Tools > Game Input > Create Config)
    /// Assign your Input Actions asset, everything else is generated from it.
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "Game Input/Config")]
    public sealed class GameInputConfig : ScriptableObject {
        public const string ResourceName = "GameInputConfig";

        [Header("Actions")]
        [Tooltip("Edit this asset in Unity's Input Actions editor (double click it). Every action map in it gets typed access in GameActions.")]
        [SerializeField] InputActionAsset actions;
        [Tooltip("Maps that are NOT wrapped in code, e.g. the UI map that the UI input module reads on its own.")]
        [SerializeField] List<string> excludedMaps = new List<string> { "UI" };

        [Header("Devices")]
        [Tooltip("Hide and lock the mouse while a gamepad is the active device.")]
        [SerializeField] bool hideMouseOnGamepadUse = true;
        [SerializeField] bool rumbleEnabled = true;

        [Header("Rebinding")]
        [Tooltip("Control scheme (binding group) names in the Input Actions asset.")]
        [SerializeField] string keyboardScheme = "Keyboard&Mouse";
        [SerializeField] string gamepadScheme = "Gamepad";
        [SerializeField] string keyboardCancelControl = "<Keyboard>/escape";
        [SerializeField] string gamepadCancelControl = "<Gamepad>/start";
        [SerializeField] RebindConflictMode conflictMode = RebindConflictMode.Swap;
        [Tooltip("Actions the player may not remap. Format: Map/Action. Mouse look, scroll and other pointer bindings are never remappable.")]
        [SerializeField] List<string> lockedActions = new List<string>();

        [Header("Code generation")]
        [SerializeField] string generatedFolder = "Assets/Generated";
        [SerializeField] bool autoGenerate = true;

        [Header("Debug")]
        [SerializeField] bool verboseLogging;

        public InputActionAsset Actions => actions;
        public bool HideMouseOnGamepadUse => hideMouseOnGamepadUse;
        public bool RumbleEnabled => rumbleEnabled;
        public string KeyboardScheme => keyboardScheme;
        public string GamepadScheme => gamepadScheme;
        public string KeyboardCancelControl => keyboardCancelControl;
        public string GamepadCancelControl => gamepadCancelControl;
        public RebindConflictMode ConflictMode => conflictMode;
        public string GeneratedFolder => generatedFolder;
        public bool AutoGenerate => autoGenerate;
        public bool VerboseLogging => verboseLogging;

        public static GameInputConfig Load() => Resources.Load<GameInputConfig>(ResourceName);

        public bool IsExcludedMap(string mapName) => excludedMaps.Contains(mapName);
        public bool IsLocked(string mapName, string actionName) => lockedActions.Contains(mapName + "/" + actionName);
    }

}
