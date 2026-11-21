using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class ToggleDisplay : SettingDisplay {
        public override string Id => "toggle";
        public override string DisplayName => "Toggle";
        public override bool Supports(SettingType type) => type == SettingType.Bool;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var toggle = Require<Toggle>(root, "toggle");
            if (toggle == null) return;

            var valueLabel = root.Q<Label>("value"); // optional "On" / "Off" text
            OnArrows(toggle, binding.Nudge);
            toggle.RegisterValueChangedCallback(e => binding.SetValue(e.newValue));

            void Refresh() {
                toggle.SetValueWithoutNotify(binding.BoolValue);
                if (valueLabel != null) valueLabel.text = binding.Format();
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
