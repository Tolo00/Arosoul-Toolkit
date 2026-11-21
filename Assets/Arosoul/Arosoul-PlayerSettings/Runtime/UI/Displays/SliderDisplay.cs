using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class SliderDisplay : SettingDisplay {
        public override string Id => "slider";
        public override string DisplayName => "Slider";
        public override bool Supports(SettingType type) => type == SettingType.Int || type == SettingType.Float;

        public override void Bind(VisualElement root, SettingBinding binding) {
            var slider = Require<Slider>(root, "slider");
            if (slider == null) return;

            var valueLabel = root.Q<Label>("value"); // optional
            OnArrows(slider, binding.Nudge);
            bool isInt = binding.Type == SettingType.Int;

            slider.lowValue = binding.Min;
            slider.highValue = binding.Max;
            slider.RegisterValueChangedCallback(e => {
                if (isInt) binding.SetValue(Mathf.RoundToInt(e.newValue));
                else binding.SetValue(e.newValue);
            });

            void Refresh() {
                slider.SetValueWithoutNotify(binding.FloatValue); // also snaps int sliders back to whole numbers
                if (valueLabel != null) valueLabel.text = binding.Format();
            }
            binding.ValueChanged += Refresh;
            Refresh();
        }
    }

}
