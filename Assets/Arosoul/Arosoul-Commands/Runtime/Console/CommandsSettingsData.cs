using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Arosoul.Commands
{
    /// <summary>
    /// Console settings. Edited in Tools > Arosoul Commands > Settings and stored in ProjectSettings/CommandsSettings.asset;
    /// the runtime reads a baked copy, see <see cref="CommandsRuntimeSettings"/>.
    /// </summary>
    [Serializable]
    public class CommandsSettingsData
    {
        [Header("Availability")]
        [Tooltip("Where the console is available. It is always available in the editor. Where it is not, the console object destroys itself on Awake.")]
        public CommandsAvailability Availability = CommandsAvailability.DevelopmentBuilds;

        [Tooltip("Keep the console alive across scene loads. The console object is moved to the scene root.")]
        public bool DontDestroyOnLoad = true;

        [Header("Keys")]
        [Tooltip("Opens and closes the console.")]
        public Key ToggleKey = Key.Backquote;

        [Tooltip("Closes the console. Set to None to only close with the toggle key.")]
        public Key CloseKey = Key.Escape;

        [Tooltip("Runs the typed command. Numpad Enter also works when this is Enter.")]
        public Key SubmitKey = Key.Enter;

        [Tooltip("Fills in the selected suggestion.")]
        public Key AutocompleteKey = Key.Tab;

        [Tooltip("Selects the previous suggestion, or the previous command in the history when no suggestions are shown.")]
        public Key PreviousKey = Key.UpArrow;

        [Tooltip("Selects the next suggestion, or the next command in the history when no suggestions are shown.")]
        public Key NextKey = Key.DownArrow;

        [Header("Behaviour")]
        [Tooltip("Set the time scale to 0 while the console is open. The previous time scale is restored on close unless a command changed it.")]
        public bool PauseWhileOpen;

        [Tooltip("Minimum level of Unity log messages shown in the console. Can be changed at runtime with the log-level command.")]
        public UnityLogLevel UnityLogLevel = UnityLogLevel.Warning;

        [Tooltip("Keep the stack trace of errors. Click an error in the console to show it.")]
        public bool ShowStackTraces = true;

        [Tooltip("Maximum number of suggestions shown while typing.")]
        [Range(1, 20)]
        public int MaxSuggestions = 8;

        [Tooltip("Maximum number of lines kept in the console log.")]
        [Range(50, 2000)]
        public int MaxLogEntries = 500;

        [Tooltip("Maximum number of commands kept in the history. 0 disables the history.")]
        [Range(0, 200)]
        public int HistorySize = 50;

        [Tooltip("Store the history in PlayerPrefs so it is kept between sessions.")]
        public bool PersistHistory = true;

        [Header("Appearance")]
        [Tooltip("Height of the console as a fraction of the screen height.")]
        [Range(0.15f, 1f)]
        public float Height = 0.4f;

        [Tooltip("Font size of the console text, in reference pixels (1920x1080).")]
        [Range(8, 32)]
        public int FontSize = 16;

        [Tooltip("Sorting order of the console panel. Higher draws on top; the default is above the Transitions overlay.")]
        public float SortingOrder = 32100f;

        [Tooltip("Optional panel settings to render the console with. When empty the console creates its own, using the theme below.")]
        public PanelSettings PanelSettings;

        [Tooltip("Theme used by the panel the console creates. Without a theme, text does not render in builds. Filled in automatically when empty.")]
        public ThemeStyleSheet Theme;

        public CommandsSettingsData Clone() => (CommandsSettingsData)MemberwiseClone();
    }
}
