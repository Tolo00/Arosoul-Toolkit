using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>Selected option with previous/next buttons on each side.</summary>
    public sealed class OptionStepperDisplay : SettingDisplay {
        public override string Id => "option-stepper";
        public override string DisplayName => "Options With Buttons";
        public override int Priority => 1;
        public override bool Supports(SettingType type) => type == SettingType.Enum;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var decrease = Require<Button>(root, "decrease");
            var increase = Require<Button>(root, "increase");
            if (decrease == null || increase == null) return;

            UseRowAsFocusTarget(root, decrease, increase);
            OnArrows(RowOf(root), binding.Nudge);

            var valueLabel = root.Q<Label>("value");
            var options = binding.Options;

            decrease.clicked += () => binding.Nudge(-1);
            increase.clicked += () => binding.Nudge(1);

            void Refresh() {
                int index = IndexOf(options, binding.StringValue);
                if (valueLabel != null) valueLabel.text = binding.Format();
                decrease.SetEnabled(index > 0);
                increase.SetEnabled(index < options.Count - 1);
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
