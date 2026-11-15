using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    public sealed class DisplayModeSetting : SettingDefinition {
        public const string Key = "display_mode";
        static readonly string[] options = { "Fullscreen", "Borderless", "Windowed" };

        public override string Id => Key;
        public override string DisplayName => "Display Mode";
        public override string Group => "Window";
        public override SettingType Type => SettingType.Enum;
        public override object DefaultValue => "Borderless";
        public override IReadOnlyList<string> Options => options;

        public override void Apply(object value) {
            string id = (string)value;
            switch (id) {
                case "Fullscreen": WindowState.SetMode(FullScreenMode.ExclusiveFullScreen); break;
                case "Windowed": WindowState.SetMode(FullScreenMode.Windowed); break;
                default: WindowState.SetMode(FullScreenMode.FullScreenWindow); break;
            }
        }
    }

}
