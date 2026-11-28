using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Arosoul.GameInput {

    /// <summary>Owns the runtime copy of the Input Actions asset and links it to the generated actions.</summary>
    public static class GameInputRuntime {
        static readonly Dictionary<string, GameAction> registered = new Dictionary<string, GameAction>();
        static GameInputRunner runner;

        internal static InputActionAsset Asset { get; private set; }
        internal static GameInputConfig Config { get; private set; }
        internal static bool IsRunning => Asset != null;

        /// <summary>Called by the generated GameActions class.</summary>
        public static void Register(IEnumerable<GameAction> actions) {
            foreach (var action in actions) registered[action.Path] = action;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Shutdown();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() => Initialize();

        static void Initialize() {
            Shutdown();

            Config = GameInputConfig.Load();
            if (Config == null || Config.Actions == null) {
                Debug.LogError("[GameInput] No GameInputConfig with an Input Actions asset found in a Resources folder. Use Tools > Arosoul > GameInput > Create Config.");
                return;
            }

            Validator.Verbose = Config.VerboseLogging;
            GameInput.RumbleEnabled = Config.RumbleEnabled;

            // A runtime copy, so remapping never modifies the asset in the project.
            Asset = Object.Instantiate(Config.Actions);

            int linked = 0;
            foreach (var action in registered.Values) {
                var inputAction = Asset.FindAction(action.Path, false);
                if (inputAction == null) {
                    Debug.LogError($"[GameInput] '{action.Path}' no longer exists in the Input Actions asset. Regenerate GameActions (Tools > Arosoul > GameInput > Generate Actions).");
                    continue;
                }
                action.Bind(inputAction);
                linked++;
            }

            GameInputBindings.LoadSaved();
            Asset.Enable();

            InputSystem.onEvent += OnInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;

            var go = new GameObject("[GameInput]") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<GameInputRunner>();

            Validator.LogVerbose($"{linked} actions linked.");
        }

        internal static void Shutdown() {
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;

            foreach (var action in registered.Values) action.Unbind();

            if (Asset != null) {
                Asset.Disable();
                Object.Destroy(Asset);
            }
            Asset = null;

            if (runner != null) Object.Destroy(runner.gameObject);
            runner = null;

            GameInputBindings.ResetState();
            GameInput.StopRumble();
            GameInput.ResetState();
        }

        internal static void ResetFrameStates() {
            foreach (var action in registered.Values) action.ResetFrameState();
        }

        // Device switching
        static void OnInputEvent(InputEventPtr eventPtr, InputDevice device) {
            var type = eventPtr.type;
            if (type != StateEvent.Type && type != DeltaStateEvent.Type) return;

            bool isGamepad = device is Gamepad;
            if (!isGamepad && !(device is Keyboard) && !(device is Mouse)) return;

            // Cheap early out: this runs for every input event.
            var expected = isGamepad ? GameInput.DeviceType.Gamepad : GameInput.DeviceType.Keyboard;
            if (expected == GameInput.ActiveDevice && (!isGamepad || device == GameInput.ActiveGamepad)) return;

            foreach (var control in eventPtr.EnumerateChangedControls(device, 0.2f)) {
                GameInput.ReportDevice(device);
                return;
            }
        }

        static void OnDeviceChange(InputDevice device, InputDeviceChange change) {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) {
                GameInput.ReportDeviceLost(device);
            }
        }
    }

    [AddComponentMenu("")]
    internal sealed class GameInputRunner : MonoBehaviour {
        void Update() => GameInput.TickRumble();
        void LateUpdate() => GameInputRuntime.ResetFrameStates();
        void OnApplicationFocus(bool focus) { if (!focus) GameInput.StopRumble(); }
        void OnApplicationQuit() => GameInput.StopRumble();
    }

}
