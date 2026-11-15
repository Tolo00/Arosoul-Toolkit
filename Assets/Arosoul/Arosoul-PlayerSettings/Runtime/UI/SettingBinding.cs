using System;
using System.Collections.Generic;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// What a display type gets to work with: read the current value, write a new one, and be told when it changed.
    /// Handles pending-vs-instant so displays don't have to care.
    /// </summary>
    public sealed class SettingBinding {
        readonly SettingsStore store;
        readonly bool instant;

        public SettingDefinition Definition { get; }

        /// <summary>Raised whenever the shown value may have changed (edit, undo, reset, code). Update the UI without notifying.</summary>
        public event Action ValueChanged;

        public SettingBinding(SettingsStore store, SettingDefinition definition, bool instant) {
            this.store = store;
            this.instant = instant;
            Definition = definition;
        }

        public string Id => Definition.Id;
        public string Label => Definition.DisplayName;
        public SettingType Type => Definition.Type;
        public float Min => Definition.Min;
        public float Max => Definition.Max;
        public float Step => Definition.Step;
        public IReadOnlyList<string> Options => Definition.Options;

        public object Value => store.GetCurrent(Id);
        public float FloatValue => Convert.ToSingle(Value);
        public int IntValue => Convert.ToInt32(Value);
        public bool BoolValue => (bool)Value;
        public string StringValue => (string)Value;

        public string Format() => Definition.FormatValue(Value);
        public string GetOptionLabel(string optionId) => Definition.GetOptionLabel(optionId);

        public void SetValue(object value) {
            if (instant) store.Set(Id, value);
            else store.SetPending(Id, value);
        }

        internal void NotifyValueChanged() => ValueChanged?.Invoke();
    }

}
