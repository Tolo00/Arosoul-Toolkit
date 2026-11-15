using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Brightness through a persistent global Volume with a Color Adjustments override.
    /// Only compiled when URP is installed (see the asmdef's version define).
    /// </summary>
    public sealed class BrightnessSetting : SettingDefinition {
        public const string Key = "brightness";
        static Volume volume;
        static ColorAdjustments adjustments;

        public override string Id => Key;
        public override string DisplayName => "Brightness";
        public override string Group => "Graphics";
        public override SettingType Type => SettingType.Float;
        public override object DefaultValue => 0.5f;
        public override string FormatValue(object value) => $"{Mathf.RoundToInt((float)value * 100f)}%";

        public override void Apply(object value) {
            EnsureVolume();
            adjustments.postExposure.Override(Mathf.Lerp(-2f, 2f, (float)value));
        }

        static void EnsureVolume() {
            if (volume != null) return;

            var go = new GameObject("[Brightness Volume]") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            adjustments = profile.Add<ColorAdjustments>(true);
            adjustments.postExposure.overrideState = true;

            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.sharedProfile = profile;
        }
    }

}
