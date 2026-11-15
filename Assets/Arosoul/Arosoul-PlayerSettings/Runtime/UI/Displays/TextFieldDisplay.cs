using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class TextFieldDisplay : SettingDisplay {
        public override string Id => "text";
        public override string DisplayName => "Text Field";
        public override bool Supports(SettingType type) => type == SettingType.String;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var field = Require<TextField>(root, "field");
            if (field == null) return;

            field.isDelayed = true; // commit on enter / focus lost instead of on every key press
            field.RegisterValueChangedCallback(e => binding.SetValue(e.newValue));

            void Refresh() => field.SetValueWithoutNotify(binding.StringValue);
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
