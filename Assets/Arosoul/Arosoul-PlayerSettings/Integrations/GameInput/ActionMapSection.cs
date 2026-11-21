using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var group in GroupEntries(GameInputBindings.GetEntries(mapName, device))) {
                var row = rowAsset.Instantiate();
                SettingsNavigation.MarkRow(row);
                var label = row.Q<Label>("label");
                var bindingsContainer = row.Q("bindings");
                var resetButton = row.Q<Button>("reset");
                if (bindingsContainer == null) { Debug.LogError("[Settings] Template 'Rebind_Row' needs an element named 'bindings'."); continue; }

                if (label != null) label.text = group[0].BaseName;

                Button firstButton = null;
                foreach (var entry in group) {
                    var bindButton = new Button();
                    bindButton.AddToClassList("rebind-button");
                    bindingsContainer.Add(bindButton);
                    firstButton ??= bindButton;
                    refreshers.Add(() => bindButton.text = GameInputBindings.GetLabel(entry));

                    bool listening = false;
                    bindButton.clicked += () => {
                        if (Time.unscaledTime < ignoreClicksUntil) return;
                        if (listening) { GameInputBindings.CancelRebind(); return; }

                        string before = GameInputBindings.GetMapJson(mapName);
                        listening = true;
                        bindButton.text = device == GameInput.DeviceType.Gamepad ? "Press a button..." : "Press a key...";
                        bindButton.AddToClassList("rebind-button-listening");

                        if (GameInputBindings.StartRebind(entry, success => {
                            SettingsNavigation.EndCapture(); // menu navigation ignores input while a key is being captured
                            listening = false;
                            bindButton.RemoveFromClassList("rebind-button-listening");
                            ignoreClicksUntil = Time.unscaledTime + 0.25f;
                            if (success) MarkChanged(before, instant);
                            RefreshAll();
                        })) SettingsNavigation.BeginCapture();
                    };
                }

                if (resetButton != null) {
                    refreshers.Add(() => resetButton.SetEnabled(group.Any(e => !GameInputBindings.IsDefault(e)))); // disabled buttons are skipped by navigation

                    resetButton.clicked += () => {
                        if (Time.unscaledTime < ignoreClicksUntil) return;
                        string before = GameInputBindings.GetMapJson(mapName);
                        foreach (var entry in group) GameInputBindings.ResetBinding(entry);
                        MarkChanged(before, instant);
                        RefreshAll();
                        firstButton?.Focus(); // the reset button just became disabled, don't leave focus on it
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

        // Bindings of the same action (and composite part) share one row, in their original order.
        static List<List<RebindEntry>> GroupEntries(List<RebindEntry> entries) {
            var groups = new List<List<RebindEntry>>();
            var lookup = new Dictionary<string, List<RebindEntry>>();

            foreach (var entry in entries) {
                string key = entry.Action.name + "/" + entry.BaseName;
                if (!lookup.TryGetValue(key, out var group)) {
                    group = new List<RebindEntry>();
                    lookup.Add(key, group);
                    groups.Add(group);
                }
                group.Add(entry);
            }
            return groups;
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
