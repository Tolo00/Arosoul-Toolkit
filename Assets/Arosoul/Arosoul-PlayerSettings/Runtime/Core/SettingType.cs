using System;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// The value kinds a setting can have. "Enum" is a choice between named options;
    /// its value is stored as the option's id (a string).
    /// </summary>
    public enum SettingType { String, Int, Float, Bool, Enum }

    /// <summary>Marks the class that the Custom Settings editor window generates.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GeneratedSettingsAttribute : Attribute { }

}
