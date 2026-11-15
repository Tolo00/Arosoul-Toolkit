using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// A way of showing a setting (slider, stepper, dropdown...). To add a new one, derive from this class:
    /// it is discovered automatically, appears in the layout editor for every type it supports, and is rendered
    /// from a UXML template named "Display_{Id}" (in Resources/PlayerSettings or the theme's display templates).
    /// 
    /// Instances are shared by all rows, so keep them stateless: put per-row state in locals captured by lambdas.
    /// </summary>
    public abstract class SettingDisplay {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract bool Supports(SettingType type);

        /// <summary>Among the displays supporting a type, the lowest priority is the default one.</summary>
        public virtual int Priority => 0;

        public virtual string TemplateName => "Display_" + Id;

        /// <summary>Wire the template instance up to the binding. The element named "label" is already filled in.</summary>
        public abstract void Bind(VisualElement root, SettingBinding binding);

        /// <summary>Finds a required element and logs a readable error if the template doesn't have it.</summary>
        protected T Require<T>(VisualElement root, string elementName) where T : VisualElement {
            var element = root.Q<T>(elementName);
            if (element == null) {
                Debug.LogError($"[Settings] Template '{TemplateName}' needs a {typeof(T).Name} named '{elementName}'.");
            }
            return element;
        }

        protected static int IndexOf(IReadOnlyList<string> list, string value) => SettingValues.IndexOf(list, value);
    }

    /// <summary>Lookup of all display types.</summary>
    public static class SettingDisplays {
        static List<SettingDisplay> all;

        public static IReadOnlyList<SettingDisplay> All {
            get {
                if (all == null) all = TypeDiscovery.CreateInstances<SettingDisplay>().OrderBy(d => d.Priority).ThenBy(d => d.DisplayName, StringComparer.Ordinal).ToList();
                return all;
            }
        }

        public static SettingDisplay Find(string id) => All.FirstOrDefault(d => d.Id == id);

        public static IEnumerable<SettingDisplay> For(SettingType type) => All.Where(d => d.Supports(type));

        public static SettingDisplay Default(SettingType type) => For(type).FirstOrDefault();
    }

}
