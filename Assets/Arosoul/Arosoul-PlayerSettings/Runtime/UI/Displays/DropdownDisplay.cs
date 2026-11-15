using System.Linq;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class DropdownDisplay : SettingDisplay {
        public override string Id => "dropdown";
        public override string DisplayName => "Dropdown";
        public override bool Supports(SettingType type) => type == SettingType.Enum;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var dropdown = Require<DropdownField>(root, "dropdown");
            if (dropdown == null) return;

            var options = binding.Options;
            var labels = options.Select(binding.GetOptionLabel).ToList();
            dropdown.choices = labels;

            dropdown.RegisterValueChangedCallback(e => {
                int index = labels.IndexOf(e.newValue);
                if (index >= 0) binding.SetValue(options[index]);
            });

            void Refresh() {
                int index = IndexOf(options, binding.StringValue);
                dropdown.SetValueWithoutNotify(index >= 0 ? labels[index] : string.Empty);
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
