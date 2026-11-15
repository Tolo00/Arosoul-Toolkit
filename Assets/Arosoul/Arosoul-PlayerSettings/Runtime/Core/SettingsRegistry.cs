using System;
using System.Collections.Generic;
using System.Linq;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// The catalog of every setting that exists: built-in definitions (found by type discovery)
    /// plus custom settings (registered by the generated CustomSettings class).
    /// </summary>
    public static class SettingsRegistry {
        static readonly Dictionary<string, CustomSettingData> customData = new Dictionary<string, CustomSettingData>(StringComparer.Ordinal);
        static List<SettingDefinition> builtIn = new List<SettingDefinition>();
        static List<SettingDefinition> all = new List<SettingDefinition>();
        static Dictionary<string, SettingDefinition> byId = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal);
        static bool built;

        public static IReadOnlyList<SettingDefinition> All { get { EnsureBuilt(); return all; } }
        public static IReadOnlyList<SettingDefinition> BuiltIn { get { EnsureBuilt(); return builtIn; } }

        public static bool TryGet(string id, out SettingDefinition definition) {
            EnsureBuilt();
            if (id == null) { definition = null; return false; }
            return byId.TryGetValue(id, out definition);
        }

        /// <summary>Called by the generated CustomSettings class.</summary>
        public static void RegisterCustom(IEnumerable<CustomSettingData> data) {
            foreach (var d in data) customData[d.apiName] = d;
            built = false;
        }

        public static void Rebuild() {
            builtIn = TypeDiscovery.CreateInstances<SettingDefinition>()
                .OrderBy(d => d.Group, StringComparer.Ordinal)
                .ThenBy(d => d.DisplayName, StringComparer.Ordinal)
                .ToList();

#if UNITY_EDITOR
            // Outside play mode the runtime registration hasn't run, so read the generated classes directly.
            foreach (var type in UnityEditor.TypeCache.GetTypesWithAttribute<GeneratedSettingsAttribute>()) {
                var field = type.GetField("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (field != null && field.GetValue(null) is CustomSettingData[] data) RegisterCustomInternal(data);
            }
#endif

            all = new List<SettingDefinition>(builtIn);
            all.AddRange(customData.Values.Select(d => (SettingDefinition)new CustomSetting(d)));

            byId = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal);
            var unique = new List<SettingDefinition>(all.Count);
            foreach (var def in all) {
                if (byId.ContainsKey(def.Id)) {
                    UnityEngine.Debug.LogWarning($"[Settings] Duplicate setting id '{def.Id}' ignored.");
                    continue;
                }
                byId.Add(def.Id, def);
                unique.Add(def);
            }
            all = unique;
            built = true;
        }

#if UNITY_EDITOR
        static void RegisterCustomInternal(IEnumerable<CustomSettingData> data) {
            foreach (var d in data) customData[d.apiName] = d;
        }
#endif

        static void EnsureBuilt() {
            if (!built) Rebuild();
        }
    }

}
