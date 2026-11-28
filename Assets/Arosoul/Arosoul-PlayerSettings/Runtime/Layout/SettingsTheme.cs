using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Optional overrides for the UXML templates and style sheets used by the menu.
    /// Any template left empty falls back to the default one in Resources/PlayerSettings.
    /// Required element names for each template are documented in the default UXML files.
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsTheme", menuName = "Arosoul/PlayerSettings/Settings Theme")]
    public sealed class SettingsTheme : ScriptableObject {

        [Serializable]
        public struct DisplayTemplate {
            [Tooltip("Id of the display type, e.g. slider, stepper, toggle, dropdown, option-stepper, text.")]
            public string displayId;
            public VisualTreeAsset template;
        }

        [Header("Structure")]
        [SerializeField] VisualTreeAsset menuTemplate;
        [SerializeField] VisualTreeAsset tabTemplate;
        [SerializeField] VisualTreeAsset categoryTemplate;
        [SerializeField] VisualTreeAsset buttonTemplate;
        [SerializeField] VisualTreeAsset promptTemplate;

        [Header("Settings rows")]
        [SerializeField] List<DisplayTemplate> displayTemplates = new List<DisplayTemplate>();

        [Header("Style")]
        [SerializeField] bool includeDefaultStyle = true;
        [SerializeField] List<StyleSheet> styleSheets = new List<StyleSheet>();

        public VisualTreeAsset MenuTemplate => menuTemplate;
        public VisualTreeAsset TabTemplate => tabTemplate;
        public VisualTreeAsset CategoryTemplate => categoryTemplate;
        public VisualTreeAsset ButtonTemplate => buttonTemplate;
        public VisualTreeAsset PromptTemplate => promptTemplate;
        public bool IncludeDefaultStyle => includeDefaultStyle;
        public IReadOnlyList<StyleSheet> StyleSheets => styleSheets;

        public VisualTreeAsset GetDisplayTemplate(string displayId) {
            foreach (var entry in displayTemplates) {
                if (entry.template != null && string.Equals(entry.displayId, displayId, StringComparison.Ordinal)) return entry.template;
            }
            return null;
        }
    }

}
