using System;
using System.Collections.Generic;
using GameInputSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// The keybinds of one action map, shown for either keyboard or gamepad. Rebinding is live (so it can be tested right away)
    /// but only saved on Apply. Undo restores the map to how it was when the first change was made.
    /// 
    /// Templates: Rebind_Section (title, rows) and Rebind_Row (label, binding, reset). Style: RebindDefault.uss.
    /// </summary>
    public sealed class ActionMapSection : SettingSection {
        const string KeyboardId = "keyboard";
        const string GamepadId = "gamepad";

        static readonly SectionDisplay[] displays = {
            new SectionDisplay(KeyboardId, "Keyboard Keybinds"),
            new SectionDisplay(GamepadId, "Gamepad Keybinds"),
        };

        // The UI would also react to the key that was just bound (e.g. Space clicking the focused button), so ignore clicks briefly.
        static float ignoreClicksUntil;

        readonly string mapName;
        string snapshot; // overrides of the map before the first unapplied change, null = nothing pending

        public ActionMapSection(string mapName) => this.mapName = mapName;

        public override string Id => "input:" + mapName;
        public override string DisplayName => mapName;
        public override string Group => "Input (Action Maps)";
        public override IReadOnlyList<SectionDisplay> Displays => displays;
        public override bool HasPending => snapshot != null;

#region UI

        public override VisualElement Build(SectionContext context) {
            var device = context.DisplayId == GamepadId ? GameInput.DeviceType.Gamepad : GameInput.DeviceType.Keyboard;
            bool instant = context.Instant;

            var sectionAsset = context.GetTemplate("Rebind_Section");
            var rowAsset = context.GetTemplate("Rebind_Row");
            if (sectionAsset == null || rowAsset == null) return null;

            var root = sectionAsset.Instantiate();
            if (context.UseDefaultStyle) {
                var style = Resources.Load<StyleSheet>("PlayerSettings/RebindDefault");
                if (style != null) root.styleSheets.Add(style);
            }

            var title = root.Q<Label>("title");
            if (title != null) title.text = $"{DisplayName} - {(device == GameInput.DeviceType.Gamepad ? "Gamepad" : "Keyboard")}";

            var rows = root.Q("rows") ?? root;
            var refreshers = new List<Action>();
            void RefreshAll() { foreach (var refresh in refreshers) refresh(); }

            foreach (var entry in GameInputBindings.GetEntries(mapName, device)) {
                var row = rowAsset.Instantiate();
                var label = row.Q<Label>("label");
                var bindButton = row.Q<Button>("binding");
                var resetButton = row.Q<Button>("reset");
                if (bindButton == null) { Debug.LogError("[Settings] Template 'Rebind_Row' needs a Button named 'binding'."); continue; }

                if (label != null) label.text = entry.DisplayName;
                refreshers.Add(() => bindButton.text = GameInputBindings.GetLabel(entry));

                bool listening = false;
                bindButton.clicked += () => {
                    if (Time.unscaledTime < ignoreClicksUntil) return;
                    if (listening) { GameInputBindings.CancelRebind(); return; }

                    string before = GameInputBindings.GetMapJson(mapName);
                    listening = true;
                    bindButton.text = device == GameInput.DeviceType.Gamepad ? "Press a button..." : "Press a key...";
                    bindButton.AddToClassList("rebind-button-listening");

                    GameInputBindings.StartRebind(entry, success => {
                        listening = false;
                        bindButton.RemoveFromClassList("rebind-button-listening");
                        ignoreClicksUntil = Time.unscaledTime + 0.25f;
                        if (success) MarkChanged(before, instant);
                        RefreshAll();
                    });
                };

                if (resetButton != null) {
                    resetButton.clicked += () => {
                        if (Time.unscaledTime < ignoreClicksUntil) return;
                        string before = GameInputBindings.GetMapJson(mapName);
                        GameInputBindings.ResetBinding(entry);
                        MarkChanged(before, instant);
                        RefreshAll();
                    };
                }

                rows.Add(row);
            }

            // Refresh when bindings change elsewhere (undo, reset) or the gamepad brand changes (button names).
            void OnBindingsChanged(string map) { if (map == mapName) RefreshAll(); }
            void OnKindChanged(GameInput.GamepadKind _) => RefreshAll();

            root.RegisterCallback<AttachToPanelEvent>(_ => {
                GameInputBindings.BindingsChanged += OnBindingsChanged;
                GameInput.OnGamepadKindChanged += OnKindChanged;
                RefreshAll();
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => {
                GameInputBindings.BindingsChanged -= OnBindingsChanged;
                GameInput.OnGamepadKindChanged -= OnKindChanged;
                GameInputBindings.CancelRebind();
            });

            RefreshAll();
            return root;
        }

#endregion

#region Pending Changes

        void MarkChanged(string before, bool instant) {
            if (snapshot == null && GameInputBindings.GetMapJson(mapName) != before) snapshot = before;

            if (instant) Commit();
            else RaiseStateChanged();
        }

        void Commit() {
            GameInputBindings.SaveMap(mapName);
            snapshot = null;
            RaiseStateChanged();
        }

        public override void ApplyPending() {
            if (snapshot != null) Commit();
        }

        public override void RevertPending() {
            if (snapshot == null) return;
            GameInputBindings.RestoreMapJson(mapName, snapshot);
            snapshot = null;
            RaiseStateChanged();
        }

        public override void ResetToDefaults(string displayId, bool applyImmediately) {
            var device = displayId == GamepadId ? GameInput.DeviceType.Gamepad : GameInput.DeviceType.Keyboard;

            string before = GameInputBindings.GetMapJson(mapName);
            foreach (var entry in GameInputBindings.GetEntries(mapName, device)) GameInputBindings.ResetBinding(entry);
            MarkChanged(before, applyImmediately);
        }

#endregion

    }
}
