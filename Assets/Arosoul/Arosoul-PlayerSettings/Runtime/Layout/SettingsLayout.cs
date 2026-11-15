using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.PlayerSettings {

    public enum TabOrientation { Horizontal, Vertical }

    public enum ApplyMode {
        /// <summary>Edits are staged. The player presses Apply (or Undo / Reset).</summary>
        Manual,
        /// <summary>Every change is applied the moment it is made.</summary>
        Instant
    }

    [Serializable]
    public sealed class EntryLayout {
        public string settingId;
        public string displayType;
    }

    [Serializable]
    public sealed class CategoryLayout {
        public string name = "New Category";
        public ApplyMode applyMode = ApplyMode.Manual;
        public bool showResetButton = true;
        public List<EntryLayout> entries = new List<EntryLayout>();
    }

    /// <summary>
    /// Describes how the settings menu is generated: which tabs exist, which settings each tab contains
    /// and how each setting is displayed. Edit the categories with the "Edit Categories" button in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsLayout", menuName = "Player Settings/Settings Layout")]
    public sealed class SettingsLayout : ScriptableObject {
        [SerializeField] string title = "Settings";
        [SerializeField] TabOrientation tabOrientation = TabOrientation.Horizontal;
        [SerializeField] bool showCloseButton = true;
        [Tooltip("Optional. Overrides the default UXML templates and styling.")]
        [SerializeField] SettingsTheme theme;
        [SerializeField] List<CategoryLayout> categories = new List<CategoryLayout>();

        public string Title => title;
        public TabOrientation TabOrientation => tabOrientation;
        public bool ShowCloseButton => showCloseButton;
        public SettingsTheme Theme => theme;
        public List<CategoryLayout> Categories => categories;
    }

}
