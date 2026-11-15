using System;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Plain data describing one custom setting. The generated CustomSettings.cs file contains an
    /// array of these, which is how the editor window reads the settings back.
    /// </summary>
    [Serializable]
    public sealed class CustomSettingData {
        public string apiName = "NewSetting";
        public string displayName = "New Setting";
        public SettingType type = SettingType.Float;
        public float min = 0f;
        public float max = 1f;
        public string defaultValue = "0";
        public string[] options = new string[0];

        public CustomSettingData Clone() {
            var copy = (CustomSettingData)MemberwiseClone();
            copy.options = options != null ? (string[])options.Clone() : new string[0];
            return copy;
        }
    }

}
