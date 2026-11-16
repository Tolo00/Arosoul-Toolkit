using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    /// <summary>
    /// Remapping API. Build your own rebind UI through this API.
    /// This is what the Arosoul Player Settings integration uses.
    /// Overrides are saved per action map, so a map can be committed or rolled back independently.
    /// </summary>
    public static class GameInputBindings {
        [Serializable] sealed class MapEntry { public string map; public string json; }
        [Serializable] sealed class FileData { public List<MapEntry> maps = new List<MapEntry>(); }

        static readonly Dictionary<string, string> saved = new Dictionary<string, string>();
        static InputActionRebindingExtensions.RebindingOperation operation;

        static string FilePath => Path.Combine(Application.persistentDataPath, "input-bindings.json");

        /// <summary>The bindings of a map changed (rebind, reset, restore). Argument is the map name.</summary>
        public static event Action<string> BindingsChanged;

        public static bool IsRebinding => operation != null;

# region Listing

        /// <summary>Names of the action maps that are wrapped in code (i.e. not excluded in the config).</summary>
        public static List<string> GetMapNames() {
            var result = new List<string>();
            if (GameInputRuntime.Asset == null) return result;

            foreach (var map in GameInputRuntime.Asset.actionMaps) {
                if (!GameInputRuntime.Config.IsExcludedMap(map.name)) result.Add(map.name);
            }
            return result;
        }

        /// <summary>Every remappable binding of a map for one device. Composites (WASD) give one entry per part.</summary>
        public static List<RebindEntry> GetEntries(string mapName, GameInput.DeviceType device) {
            var result = new List<RebindEntry>();
            var map = GameInputRuntime.Asset != null ? GameInputRuntime.Asset.FindActionMap(mapName, false) : null;
            if (map == null) return result;

            foreach (var action in map.actions) {
                if (GameInputRuntime.Config.IsLocked(mapName, action.name)) continue;

                var names = new Dictionary<string, int>();
                var bindings = action.bindings;

                for (int i = 0; i < bindings.Count; i++) {
                    var b = bindings[i];
                    if (b.isComposite || !BelongsToDevice(b, device) || IsNotRemappable(b)) continue;

                    string name = NameUtil.Nicify(action.name);
                    if (b.isPartOfComposite) name += " " + NameUtil.Nicify(b.name);

                    names.TryGetValue(name, out int count);
                    names[name] = count + 1;
                    if (count > 0) name += " #" + (count + 1);

                    result.Add(new RebindEntry { MapName = mapName, Action = action, BindingIndex = i, Device = device, DisplayName = name });
                }
            }
            return result;
        }

#endregion

#region Labels

        /// <summary>Current control of one binding as text, e.g. "Space", "Left Button", "Cross".</summary>
        public static string GetLabel(RebindEntry entry) {
            return FormatPath(entry.Action.bindings[entry.BindingIndex].effectivePath);
        }

        /// <summary>Label of an action for a device (first matching binding, composites as "W/A/S/D").</summary>
        public static string GetActionLabel(GameAction gameAction, GameInput.DeviceType device) {
            var action = gameAction.InputAction;
            if (action == null) return string.Empty;

            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++) {
                var b = bindings[i];
                if (b.isPartOfComposite) continue;

                if (b.isComposite) {
                    if (i + 1 < bindings.Count && BelongsToDevice(bindings[i + 1], device)) return action.GetBindingDisplayString(i);
                    continue;
                }
                if (BelongsToDevice(b, device) && !string.IsNullOrEmpty(b.effectivePath)) return FormatPath(b.effectivePath);
            }
            return string.Empty;
        }

        static string FormatPath(string path) {
            if (string.IsNullOrEmpty(path)) return "Unbound";
            return GamepadGlyphs.Format(path, GameInput.ActiveGamepadKind);
        }

#endregion

#region Rebinding

        /// <param name="finished">Called with true if a new control was assigned, false if cancelled or timed out.</param>
        public static bool StartRebind(RebindEntry entry, Action<bool> finished = null) {
            if (GameInputRuntime.Asset == null || entry == null) return false;
            CancelRebind();

            var config = GameInputRuntime.Config;
            var action = entry.Action;
            bool wasEnabled = action.enabled;
            action.Disable(); // actions can't be rebound while enabled

            string oldPath = action.bindings[entry.BindingIndex].effectivePath;
            bool gamepad = entry.Device == GameInput.DeviceType.Gamepad;

            var op = action.PerformInteractiveRebinding(entry.BindingIndex)
                .WithTimeout(10f)
                .OnMatchWaitForAnother(0.1f)
                .WithCancelingThrough(gamepad ? config.GamepadCancelControl : config.KeyboardCancelControl)
                .OnComplete(_ => Finish(entry, wasEnabled, oldPath, true, finished))
                .OnCancel(_ => Finish(entry, wasEnabled, oldPath, false, finished));

            // A keyboard binding can only become a keyboard/mouse control, a gamepad binding a gamepad control.
            if (gamepad) {
                op.WithControlsHavingToMatchPath("<Gamepad>");
            }
            else {
                op.WithControlsHavingToMatchPath("<Keyboard>");
                op.WithControlsHavingToMatchPath("<Mouse>");
                op.WithControlsExcluding("<Mouse>/position");
                op.WithControlsExcluding("<Mouse>/delta");
                op.WithControlsExcluding("<Mouse>/scroll");
                op.WithControlsExcluding("<Pointer>/position");
                op.WithControlsExcluding("<Pointer>/delta");
            }

            operation = op;
            op.Start();
            return true;
        }

        public static void CancelRebind() => operation?.Cancel();

        static void Finish(RebindEntry entry, bool wasEnabled, string oldPath, bool success, Action<bool> finished) {
            operation?.Dispose();
            operation = null;

            if (wasEnabled) entry.Action.Enable();

            if (success) {
                string newPath = entry.Action.bindings[entry.BindingIndex].effectivePath;
                if (GameInputRuntime.Config.ConflictMode == RebindConflictMode.Swap) ResolveConflicts(entry, oldPath, newPath);
                BindingsChanged?.Invoke(entry.MapName);
            }

            finished?.Invoke(success);
        }

        // If another action in the same map and device already uses the new control, it takes over the old one.
        static void ResolveConflicts(RebindEntry entry, string oldPath, string newPath) {
            if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) return;

            foreach (var other in entry.Action.actionMap.actions) {
                if (GameInputRuntime.Config.IsLocked(entry.MapName, other.name)) continue;

                var bindings = other.bindings;
                for (int i = 0; i < bindings.Count; i++) {
                    if (other == entry.Action && i == entry.BindingIndex) continue;

                    var b = bindings[i];
                    if (b.isComposite || !BelongsToDevice(b, entry.Device) || IsNotRemappable(b)) continue;
                    if (!string.Equals(b.effectivePath, newPath, StringComparison.OrdinalIgnoreCase)) continue;

                    other.ApplyBindingOverride(i, oldPath ?? string.Empty);
                }
            }
        }

#endregion

#region Reset / snapshot / persistence

        public static void ResetBinding(RebindEntry entry) {
            entry.Action.RemoveBindingOverride(entry.BindingIndex);
            BindingsChanged?.Invoke(entry.MapName);
        }

        public static void ResetMap(string mapName) {
            var map = GameInputRuntime.Asset?.FindActionMap(mapName, false);
            if (map == null) return;
            map.RemoveAllBindingOverrides();
            BindingsChanged?.Invoke(mapName);
        }

        /// <summary>Current overrides of a map as JSON. Pair with RestoreMapJson to roll back.</summary>
        public static string GetMapJson(string mapName) {
            var map = GameInputRuntime.Asset?.FindActionMap(mapName, false);
            return map != null ? map.SaveBindingOverridesAsJson() : string.Empty;
        }

        public static void RestoreMapJson(string mapName, string json) {
            var map = GameInputRuntime.Asset?.FindActionMap(mapName, false);
            if (map == null) return;

            if (string.IsNullOrEmpty(json)) map.RemoveAllBindingOverrides();
            else map.LoadBindingOverridesFromJson(json);
            BindingsChanged?.Invoke(mapName);
        }

        /// <summary>Writes the map's current overrides to disk.</summary>
        public static void SaveMap(string mapName) {
            saved[mapName] = GetMapJson(mapName);

            var data = new FileData();
            foreach (var kv in saved) data.maps.Add(new MapEntry { map = kv.Key, json = kv.Value });

            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
            catch (Exception e) { Debug.LogException(e); }
        }

        internal static void LoadSaved() {
            saved.Clear();
            if (!File.Exists(FilePath)) return;

            try {
                var data = JsonUtility.FromJson<FileData>(File.ReadAllText(FilePath));
                if (data?.maps == null) return;

                foreach (var entry in data.maps) {
                    var map = GameInputRuntime.Asset.FindActionMap(entry.map, false);
                    if (map == null || string.IsNullOrEmpty(entry.json)) continue;

                    saved[entry.map] = entry.json;
                    map.LoadBindingOverridesFromJson(entry.json);
                }
            }
            catch (Exception e) {
                Debug.LogWarning($"[GameInput] Could not read saved bindings, using defaults. {e.Message}");
            }
        }

        internal static void ResetState() {
            operation?.Dispose();
            operation = null;
            saved.Clear();
        }

#endregion

#region Binding Classification

        static bool BelongsToDevice(InputBinding b, GameInput.DeviceType device) {
            var config = GameInputRuntime.Config;
            string scheme = device == GameInput.DeviceType.Gamepad ? config.GamepadScheme : config.KeyboardScheme;

            if (!string.IsNullOrEmpty(b.groups)) {
                foreach (string group in b.groups.Split(';')) if (group == scheme) return true;
                return false;
            }

            // No group assigned: decide by the device in the path.
            string path = b.path ?? string.Empty;
            return device == GameInput.DeviceType.Gamepad
                ? path.StartsWith("<Gamepad>", StringComparison.Ordinal)
                : path.StartsWith("<Keyboard>", StringComparison.Ordinal) || path.StartsWith("<Mouse>", StringComparison.Ordinal);
        }

        // Pointer movement, scroll and usage based paths (*/{Submit}) make no sense to remap by pressing something.
        static bool IsNotRemappable(InputBinding b) {
            string p = b.path ?? string.Empty;
            return p.Contains("{") || p.EndsWith("/delta") || p.EndsWith("/position")
                || p.StartsWith("<Mouse>/scroll", StringComparison.Ordinal) || p.StartsWith("<Pointer>", StringComparison.Ordinal);
        }

#endregion

    }
}
