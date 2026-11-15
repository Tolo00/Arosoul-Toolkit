using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>Choice between the quality levels defined in Project Settings > Quality.</summary>
    public sealed class QualityLevelSetting : SettingDefinition {
        public const string Key = "quality_level";

        public override string Id => Key;
        public override string DisplayName => "Quality";
        public override string Group => "Graphics";
        public override SettingType Type => SettingType.Enum;
        public override int Order => -100; // Changing the level resets vsync, so it must be applied before it.
        public override IReadOnlyList<string> Options => QualitySettings.names;
        public override string GetOptionLabel(string optionId) => optionId;

        public override object DefaultValue {
            get {
                var names = QualitySettings.names;
                int level = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, names.Length - 1);
                return names[level];
            }
        }

        public override void Apply(object value) {
            int index = System.Array.IndexOf(QualitySettings.names, (string)value);
            if (index >= 0) QualitySettings.SetQualityLevel(index, true);
        }
    }

}
