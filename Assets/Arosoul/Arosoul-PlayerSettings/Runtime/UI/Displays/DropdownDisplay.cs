using System.Linq;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class DropdownDisplay : SettingDisplay {
        public override string Id => "dropdown";
        public override string DisplayName => "Dropdown";
        public override bool Supports(SettingType type) => type == SettingType.Enum;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var field = Require<VisualElement>(root, "dropdown");
            if (field == null) return;

            var button = new Button { name = "dropdown" };
            foreach (var c in field.GetClasses()) button.AddToClassList(c);
            button.AddToClassList("setting-dropdown-button");
            field.parent.Insert(field.parent.IndexOf(field), button);
            field.RemoveFromHierarchy();

            var options = binding.Options;
            var labels = options.Select(binding.GetOptionLabel).ToList();

            button.clicked += () => DropdownPopup.Open(button, labels, IndexOf(options, binding.StringValue), i => binding.SetValue(options[i]));
            OnArrows(button, binding.Nudge);

            void Refresh() {
                int index = IndexOf(options, binding.StringValue);
                button.text = index >= 0 ? labels[index] : string.Empty;
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
