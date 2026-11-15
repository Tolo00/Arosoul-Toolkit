using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    public sealed class FrameRateSetting : SettingDefinition {
        public const string Key = "frame_rate_limit";
        const string Unlimited = "unlimited";
        static readonly string[] options = { "30", "60", "120", "144", "240", Unlimited };

        public override string Id => Key;
        public override string DisplayName => "Frame Rate Limit";
        public override string Group => "Window";
        public override SettingType Type => SettingType.Enum;
        public override object DefaultValue => Unlimited;
        public override IReadOnlyList<string> Options => options;

        public override string GetOptionLabel(string optionId) => optionId == Unlimited ? "Unlimited" : optionId + " FPS";

        public override void Apply(object value) {
            string id = (string)value;
            Application.targetFrameRate = id == Unlimited ? -1 : int.Parse(id);
        }
    }

}
