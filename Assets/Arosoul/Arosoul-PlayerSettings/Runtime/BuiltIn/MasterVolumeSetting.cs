using UnityEngine;

namespace Arosoul.PlayerSettings {

    public sealed class MasterVolumeSetting : SettingDefinition {
        public const string Key = "master_volume";

        public override string Id => Key;
        public override string DisplayName => "Master Volume";
        public override string Group => "Audio";
        public override SettingType Type => SettingType.Float;
        public override object DefaultValue => 0.8f;

        public override string FormatValue(object value) => $"{Mathf.RoundToInt((float)value * 100f)}%";
        public override void Apply(object value) => AudioListener.volume = (float)value;
    }

}
