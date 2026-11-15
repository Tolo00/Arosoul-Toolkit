using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>Choice between the resolutions the display supports. The value is stored as "1920x1080".</summary>
    public sealed class ResolutionSetting : SettingDefinition {
        public const string Key = "resolution";
        List<string> options;

        public override string Id => Key;
        public override string DisplayName => "Resolution";
        public override string Group => "Window";
        public override SettingType Type => SettingType.Enum;
        public override IReadOnlyList<string> Options => options ??= BuildOptions();
        public override string GetOptionLabel(string optionId) => optionId.Replace("x", " x ");

        public override object DefaultValue {
            get {
                var current = Screen.currentResolution;
                string id = Format(current.width, current.height);
                return SettingValues.IndexOf(Options, id) >= 0 ? id : Options[Options.Count - 1];
            }
        }

        public override void Apply(object value) {
            var parts = ((string)value).Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)) {
                WindowState.SetSize(w, h);
            }
        }

        static string Format(int w, int h) => $"{w}x{h}";

        static List<string> BuildOptions() {
            // Screen.resolutions contains one entry per refresh rate, we only want distinct sizes.
            var list = Screen.resolutions
                .Select(r => (r.width, r.height))
                .Distinct()
                .OrderBy(r => r.width).ThenBy(r => r.height)
                .Select(r => Format(r.width, r.height))
                .ToList();

            if (list.Count == 0) list.Add(Format(Screen.width, Screen.height));
            return list;
        }
    }

}
