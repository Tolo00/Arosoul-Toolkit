using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Describes one setting: what it is, its range/options, how it is formatted and how it is applied to the game.
    /// 
    /// Derive from this class (public, non-abstract, public parameterless constructor) to add a built-in setting with custom apply logic. 
    /// It is discovered automatically and shows up in the layout editor.
    /// Keep constructors free of Unity API calls, definitions are also created in edit mode.
    /// </summary>
    public abstract class SettingDefinition {
        static readonly IReadOnlyList<string> NoOptions = new string[0];

        /// <summary>Unique key. Used for persistence and for the layout asset.</summary>
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract SettingType Type { get; }
        public abstract object DefaultValue { get; }

        /// <summary>Heading in the layout editor (Graphics, Window, Audio, Custom...).</summary>
        public virtual string Group => "Other";

        /// <summary>Lower values are applied first (e.g. quality level before vsync).</summary>
        public virtual int Order => 0;

        /// <summary>Return false to hide the setting on this platform.</summary>
        public virtual bool IsAvailable => true;

        public virtual float Min => 0f;
        public virtual float Max => 1f;

        /// <summary>Increment used by stepper style displays.</summary>
        public virtual float Step => Type == SettingType.Int ? 1f : Mathf.Max((Max - Min) / 20f, 0.0001f);

        /// <summary>Option ids for Enum settings. May be computed lazily (resolutions etc.).</summary>
        public virtual IReadOnlyList<string> Options => NoOptions;

        public virtual string GetOptionLabel(string optionId) => SettingValues.Nicify(optionId);

        public virtual string FormatValue(object value) {
            if (value == null) return string.Empty;
            switch (Type) {
                case SettingType.Float: return ((float)value).ToString("0.##", CultureInfo.CurrentCulture);
                case SettingType.Int: return ((int)value).ToString(CultureInfo.CurrentCulture);
                case SettingType.Bool: return (bool)value ? "On" : "Off";
                case SettingType.Enum: return GetOptionLabel((string)value);
                default: return (string)value;
            }
        }

        /// <summary>Push a (canonical, validated) value into the game. Called on startup and whenever the value is committed.</summary>
        public virtual void Apply(object value) { }
    }

    /// <summary>A setting created from the Custom Settings window. Has no apply logic, other systems read it.</summary>
    public sealed class CustomSetting : SettingDefinition {
        readonly CustomSettingData data;
        readonly object defaultValue;

        public CustomSetting(CustomSettingData data) {
            this.data = data;
            defaultValue = ResolveDefault();
        }

        public override string Id => data.apiName;
        public override string DisplayName => string.IsNullOrWhiteSpace(data.displayName) ? SettingValues.Nicify(data.apiName) : data.displayName;
        public override SettingType Type => data.type;
        public override string Group => "Custom";
        public override float Min => data.min;
        public override float Max => data.max;
        public override IReadOnlyList<string> Options => data.options ?? new string[0];
        public override object DefaultValue => defaultValue;

        object ResolveDefault() {
            if (SettingValues.TryParse(Type, data.defaultValue, out var parsed) && SettingValues.TryCoerce(this, parsed, out var coerced)) {
                return coerced;
            }

            switch (Type) {
                case SettingType.Float: return Mathf.Clamp(0f, Min, Max);
                case SettingType.Int: return Mathf.Clamp(0, Mathf.RoundToInt(Min), Mathf.RoundToInt(Max));
                case SettingType.Bool: return false;
                case SettingType.Enum: return Options.Count > 0 ? Options[0] : string.Empty;
                default: return string.Empty;
            }
        }
    }

}
