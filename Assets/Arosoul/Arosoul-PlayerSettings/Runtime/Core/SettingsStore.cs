using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Holds the applied value of every setting plus a set of pending (not yet applied) edits.
    /// The menu edits pending values, code that calls Set() applies immediately.
    /// </summary>
    public sealed class SettingsStore {
        readonly List<SettingDefinition> definitions = new List<SettingDefinition>();
        readonly Dictionary<string, SettingDefinition> byId = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, object> applied = new Dictionary<string, object>(StringComparer.Ordinal);
        readonly Dictionary<string, object> pending = new Dictionary<string, object>(StringComparer.Ordinal);
        readonly ISettingsStorage storage;
        bool saveRequested;

        /// <summary>An applied value changed. Argument is the setting id.</summary>
        public event Action<string> Changed;

        /// <summary>A batch of values was applied (apply button, reset, load).</summary>
        public event Action Applied;

        /// <summary>Anything the UI shows may be out of date (pending or applied values changed).</summary>
        public event Action StateChanged;

        public bool HasPending => pending.Count > 0;
        public bool SaveRequested => saveRequested;
        public IReadOnlyList<SettingDefinition> Definitions => definitions;

        public SettingsStore(IEnumerable<SettingDefinition> source, ISettingsStorage storage) {
            this.storage = storage;

            foreach (var def in source.Where(d => d.IsAvailable).OrderBy(d => d.Order)) {
                if (byId.ContainsKey(def.Id)) continue;
                byId.Add(def.Id, def);
                definitions.Add(def);
            }
        }

        public bool TryGetDefinition(string id, out SettingDefinition definition) {
            if (id == null) { definition = null; return false; }
            return byId.TryGetValue(id, out definition);
        }

#region Loading / saving

        public void Load() {
            var raw = new Dictionary<string, string>();
            bool hadFile = storage.TryLoad(raw);

            foreach (var def in definitions) {
                object value = GetDefault(def);
                if (raw.TryGetValue(def.Id, out var text)
                    && SettingValues.TryParse(def.Type, text, out var parsed)
                    && SettingValues.TryCoerce(def, parsed, out var coerced)) {
                    value = coerced;
                }
                applied[def.Id] = value;
            }

            foreach (var def in definitions) SafeApply(def, applied[def.Id]);

            saveRequested = !hadFile;
            Applied?.Invoke();
            StateChanged?.Invoke();
        }

        public void Flush() {
            if (!saveRequested) return;
            saveRequested = false;

            var values = new Dictionary<string, string>();
            foreach (var def in definitions) values[def.Id] = SettingValues.Serialize(def.Type, applied[def.Id]);

            try { storage.Save(values); }
            catch (Exception e) { Debug.LogException(e); }
        }

#endregion

#region Reading

        public object GetApplied(string id) => applied.TryGetValue(id, out var v) ? v : null;

        /// <summary>The pending value if there is one, otherwise the applied value. This is what a menu should show.</summary>
        public object GetCurrent(string id) => pending.TryGetValue(id, out var v) ? v : GetApplied(id);

        public bool IsPending(string id) => pending.ContainsKey(id);

#endregion

#region Writing

        /// <summary>Stage a value without applying it (menu edits in manual mode).</summary>
        public bool SetPending(string id, object value) {
            if (!TryPrepare(id, value, out var def, out var coerced)) return false;

            if (SettingValues.AreEqual(def.Type, coerced, applied[id])) pending.Remove(id);
            else pending[id] = coerced;

            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Apply a value right away (code, or menu in instant mode).</summary>
        public bool Set(string id, object value) {
            if (!TryPrepare(id, value, out var def, out var coerced)) return false;

            pending.Remove(id);
            if (!SettingValues.AreEqual(def.Type, coerced, applied[id])) {
                applied[id] = coerced;
                SafeApply(def, coerced);
                saveRequested = true;
                Changed?.Invoke(id);
            }

            StateChanged?.Invoke();
            return true;
        }

        public void ApplyPending() {
            if (pending.Count == 0) return;

            var toApply = definitions.Where(d => pending.ContainsKey(d.Id)).ToList();
            foreach (var def in toApply) applied[def.Id] = pending[def.Id];
            pending.Clear();

            foreach (var def in toApply) SafeApply(def, applied[def.Id]);
            saveRequested = true;

            foreach (var def in toApply) Changed?.Invoke(def.Id);
            Applied?.Invoke();
            StateChanged?.Invoke();
        }

        public void RevertPending() {
            if (pending.Count == 0) return;
            pending.Clear();
            StateChanged?.Invoke();
        }

        /// <param name="applyImmediately">False stages the defaults so the player can review them and press Apply.</param>
        public void ResetToDefaults(bool applyImmediately) {
            pending.Clear();
            var changed = new List<SettingDefinition>();

            foreach (var def in definitions) {
                object d = GetDefault(def);
                if (SettingValues.AreEqual(def.Type, d, applied[def.Id])) continue;

                if (applyImmediately) { applied[def.Id] = d; changed.Add(def); }
                else pending[def.Id] = d;
            }

            foreach (var def in changed) SafeApply(def, applied[def.Id]);
            if (changed.Count > 0) {
                saveRequested = true;
                foreach (var def in changed) Changed?.Invoke(def.Id);
                Applied?.Invoke();
            }
            StateChanged?.Invoke();
        }

#endregion

#region Internals

        bool TryPrepare(string id, object value, out SettingDefinition def, out object coerced) {
            coerced = null;
            if (!TryGetDefinition(id, out def)) {
                Debug.LogError($"[Settings] Unknown setting '{id}'.");
                return false;
            }
            if (!SettingValues.TryCoerce(def, value, out coerced)) {
                Debug.LogError($"[Settings] '{value}' is not a valid value for '{id}' ({def.Type}).");
                return false;
            }
            return true;
        }

        static object GetDefault(SettingDefinition def) {
            return SettingValues.TryCoerce(def, def.DefaultValue, out var v) ? v : def.DefaultValue;
        }

        static void SafeApply(SettingDefinition def, object value) {
            // Never touch screen/quality/audio from edit mode tooling.
            if (!Application.isPlaying) return;

            try { def.Apply(value); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

#endregion

}
