using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>Value with a decrease and an increase button on each side.</summary>
    public sealed class StepperDisplay : SettingDisplay {
        public override string Id => "stepper";
        public override string DisplayName => "Value With Buttons";
        public override int Priority => 1;
        public override bool Supports(SettingType type) => type == SettingType.Int || type == SettingType.Float;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var decrease = Require<Button>(root, "decrease");
            var increase = Require<Button>(root, "increase");
            if (decrease == null || increase == null) return;

            UseRowAsFocusTarget(root, decrease, increase);
            OnArrows(RowOf(root), binding.Nudge);

            var valueLabel = root.Q<Label>("value");
            bool isInt = binding.Type == SettingType.Int;
            float step = binding.Step;

            decrease.clicked += () => binding.Nudge(-1);
            increase.clicked += () => binding.Nudge(1);

            void Refresh() {
                float v = binding.FloatValue;
                float epsilon = step * 0.001f;
                if (valueLabel != null) valueLabel.text = binding.Format();
                decrease.SetEnabled(v > binding.Min + epsilon);
                increase.SetEnabled(v < binding.Max - epsilon);
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
