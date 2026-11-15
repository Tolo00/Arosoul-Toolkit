using UnityEngine;

namespace Arosoul.PlayerSettings {

    public sealed class VSyncSetting : SettingDefinition {
        public const string Key = "vsync";

        public override string Id => Key;
        public override string DisplayName => "VSync";
        public override string Group => "Window";
        public override SettingType Type => SettingType.Bool;
        public override object DefaultValue => true;

        public override void Apply(object value) => QualitySettings.vSyncCount = (bool)value ? 1 : 0;
    }

}
