using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

namespace Arosoul.Commands
{
    /// <summary>
    /// The in-game console overlay. Place the Commands Console prefab (GameObject > Arosoul > Commands Console) in your first scene.
    /// Reads its settings from <see cref="CommandsRuntimeSettings"/> and destroys itself where the console is not available.
    /// Only one console exists at a time; later ones destroy themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public class CommandConsole : MonoBehaviour
    {
        [SerializeField, Tooltip("Open the console as soon as it starts.")]
        bool _openOnStart;

        private const string HistoryPrefsKey = "Arosoul.Commands.History";

        // Setup
        private CommandsSettingsData _settings;
        private CommandHistory _history;
        private CommandConsoleView _view;
        private UIDocument _document;
        private PanelSettings _createdPanelSettings;

        // Suggestions
        private readonly List<CommandInfo> _suggestions = new();
        private int _selectedSuggestion;
        private bool _suggestionsSelectable;
        private bool _suppressSuggestions; // while showing a command recalled from the history

        // Pause
        private bool _pausedByConsole;
        private float _timeScaleBeforePause = 1f;

        private bool HasSelectableSuggestions => _suggestionsSelectable && _suggestions.Count > 0;

        /// <summary>
        /// The active console, or null.
        /// </summary>
        public static CommandConsole Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public CommandHistory History => _history;

        public event Action OnOpened;
        public event Action OnClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _settings = CommandsRuntimeSettings.CurrentData;
            if (!CommandsRuntimeSettings.IsAvailable(_settings.Availability))
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (_settings.DontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            CommandAPI.UnityLogLevel = _settings.UnityLogLevel;
            CommandAPI.Output.MaxEntries = _settings.MaxLogEntries;
            _history = new CommandHistory(_settings.HistorySize, _settings.PersistHistory ? HistoryPrefsKey : null);

            CreateDocument();
            _view = new CommandConsoleView(_settings, IsConsoleKeyHeld);
            foreach (CommandLogEntry entry in CommandAPI.Output.Entries)
                _view.AddEntry(entry);
        }

        private void OnEnable()
        {
            if (Instance != this) return;

            CommandAPI.Output.OnEntryAdded += Output_OnEntryAdded;
            CommandAPI.Output.OnCleared += Output_OnCleared;
            Application.logMessageReceived += Application_OnLogMessageReceived;
            _view.OnInputChanged += View_OnInputChanged;
            _view.OnSuggestionClicked += View_OnSuggestionClicked;

            AttachView();
        }

        private void Start()
        {
            if (Instance == this && _openOnStart) Open();
        }

        private void Update()
        {
            if (Instance != this) return;

            // The document recreates its root when it is re-enabled
            if (_view.Root.panel == null) AttachView();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (WasPressed(keyboard, _settings.ToggleKey))
            {
                Toggle();
                return;
            }

            if (!IsOpen) return;

            if (WasPressed(keyboard, _settings.CloseKey)) Close();
            else if (WasSubmitPressed(keyboard)) Submit();
            else if (WasPressed(keyboard, _settings.AutocompleteKey)) ApplySelectedSuggestion();
            else if (WasPressed(keyboard, _settings.PreviousKey)) SelectPrevious();
            else if (WasPressed(keyboard, _settings.NextKey)) SelectNext();
        }

        private void OnDisable()
        {
            if (Instance != this) return;

            CommandAPI.Output.OnEntryAdded -= Output_OnEntryAdded;
            CommandAPI.Output.OnCleared -= Output_OnCleared;
            Application.logMessageReceived -= Application_OnLogMessageReceived;
            _view.OnInputChanged -= View_OnInputChanged;
            _view.OnSuggestionClicked -= View_OnSuggestionClicked;

            Close();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_createdPanelSettings != null) Destroy(_createdPanelSettings);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        #region Public API

        public void Open()
        {
            if (IsOpen || _view == null) return;

            IsOpen = true;
            _view.SetOpen(true);
            _view.FocusInput();
            RefreshSuggestions(_view.InputText);

            if (_settings.PauseWhileOpen)
            {
                _timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
                _pausedByConsole = true;
            }

            OnOpened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            _view.SetOpen(false);
            _view.BlurInput();
            _history.ResetNavigation();

            // Keep a time scale a command set while the console was open
            if (_pausedByConsole && Time.timeScale == 0f) Time.timeScale = _timeScaleBeforePause;
            _pausedByConsole = false;

            OnClosed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        /// <summary>
        /// Run a command as if it was typed, and add it to the history.
        /// </summary>
        public bool Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            _history.Add(input);
            return CommandAPI.Execute(input);
        }

        #endregion

        #region Input

        private void Submit()
        {
            string input = _view.InputText.Trim();
            _view.SetInput(string.Empty, false);
            _suppressSuggestions = false;
            ShowSuggestions(Array.Empty<CommandInfo>(), false);

            Execute(input);
        }

        private void SelectPrevious()
        {
            if (HasSelectableSuggestions)
            {
                _selectedSuggestion = (_selectedSuggestion - 1 + _suggestions.Count) % _suggestions.Count;
                _view.SetSelectedSuggestion(_selectedSuggestion);
            }
            else if (_history.TryGetPrevious(_view.InputText, out string command))
            {
                ShowHistoryEntry(command);
            }
        }

        private void SelectNext()
        {
            if (HasSelectableSuggestions)
            {
                _selectedSuggestion = (_selectedSuggestion + 1) % _suggestions.Count;
                _view.SetSelectedSuggestion(_selectedSuggestion);
            }
            else if (_history.TryGetNext(out string command))
            {
                ShowHistoryEntry(command);
            }
        }

        private void ShowHistoryEntry(string command)
        {
            _suppressSuggestions = true;
            _view.SetInput(command, false);
            ShowSuggestions(Array.Empty<CommandInfo>(), false);
        }

        private bool IsConsoleKeyHeld()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return IsHeld(keyboard, _settings.ToggleKey) ||
                   IsHeld(keyboard, _settings.CloseKey) ||
                   IsHeld(keyboard, _settings.SubmitKey) ||
                   (_settings.SubmitKey == Key.Enter && IsHeld(keyboard, Key.NumpadEnter)) ||
                   IsHeld(keyboard, _settings.AutocompleteKey) ||
                   IsHeld(keyboard, _settings.PreviousKey) ||
                   IsHeld(keyboard, _settings.NextKey);
        }

        private bool WasSubmitPressed(Keyboard keyboard)
        {
            return WasPressed(keyboard, _settings.SubmitKey) ||
                   (_settings.SubmitKey == Key.Enter && WasPressed(keyboard, Key.NumpadEnter));
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            KeyControl control = GetKey(keyboard, key);
            return control != null && control.wasPressedThisFrame;
        }

        private static bool IsHeld(Keyboard keyboard, Key key)
        {
            KeyControl control = GetKey(keyboard, key);
            return control != null && control.isPressed;
        }

        private static KeyControl GetKey(Keyboard keyboard, Key key)
        {
            return key == Key.None ? null : keyboard[key];
        }

        #endregion

        #region Suggestions

        private void RefreshSuggestions(string text)
        {
            if (_suppressSuggestions || string.IsNullOrWhiteSpace(text))
            {
                ShowSuggestions(Array.Empty<CommandInfo>(), false);
                return;
            }

            string name = CommandTokenizer.GetCommandName(text);
            bool typingArguments = text.TrimStart().Length > name.Length;
            if (typingArguments)
            {
                // Show the usage of the command being typed
                ShowSuggestions(CommandAPI.Registry.Find(name), false);
            }
            else
            {
                ShowSuggestions(CommandAutocomplete.GetSuggestions(CommandAPI.Registry, name, _settings.MaxSuggestions), true);
            }
        }

        private void ShowSuggestions(IReadOnlyList<CommandInfo> commands, bool selectable)
        {
            _suggestions.Clear();
            _suggestions.AddRange(commands);
            _suggestionsSelectable = selectable;
            _selectedSuggestion = 0;
            _view.ShowSuggestions(_suggestions, selectable);
        }

        private void ApplySelectedSuggestion()
        {
            if (!HasSelectableSuggestions) return;

            CommandInfo command = _suggestions[_selectedSuggestion];
            string text = command.Parameters.Count > 0 ? $"{command.Name} " : command.Name;
            _view.SetInput(text, true);
        }

        #endregion

        #region Setup

        private void CreateDocument()
        {
            PanelSettings panelSettings = _settings.PanelSettings;
            if (panelSettings == null)
            {
                _createdPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _createdPanelSettings.name = "[CommandsPanelSettings]";
                _createdPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _createdPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _createdPanelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                _createdPanelSettings.match = 0.5f;
                _createdPanelSettings.sortingOrder = _settings.SortingOrder;
                if (_settings.Theme != null) _createdPanelSettings.themeStyleSheet = _settings.Theme;
                panelSettings = _createdPanelSettings;
            }

            // Assign the panel settings before activating, so the root element is created with them
            var documentObject = new GameObject("[CommandsConsoleDocument]");
            documentObject.SetActive(false);
            documentObject.transform.SetParent(transform, false);
            _document = documentObject.AddComponent<UIDocument>();
            _document.panelSettings = panelSettings;
            _document.sortingOrder = 1000f;
            documentObject.SetActive(true);
        }

        private void AttachView()
        {
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null || _view.Root.parent == root) return;

            root.pickingMode = PickingMode.Ignore;
            root.Add(_view.Root);
        }

        #endregion

        #region Events

        private void Output_OnEntryAdded(CommandLogEntry entry)
        {
            _view.AddEntry(entry);
        }

        private void Output_OnCleared()
        {
            _view.ClearEntries();
        }

        private void View_OnInputChanged(string text)
        {
            _suppressSuggestions = false;
            _history.ResetNavigation();
            RefreshSuggestions(text);
        }

        private void View_OnSuggestionClicked(int index)
        {
            if (!HasSelectableSuggestions || index < 0 || index >= _suggestions.Count) return;

            _selectedSuggestion = index;
            ApplySelectedSuggestion();
        }

        private void Application_OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (!ShouldMirror(type)) return;

            CommandLogType logType = type switch
            {
                LogType.Log => CommandLogType.Output,
                LogType.Warning => CommandLogType.Warning,
                _ => CommandLogType.Error
            };
            string trace = _settings.ShowStackTraces && logType == CommandLogType.Error ? stackTrace : null;
            CommandAPI.Output.Add(message, logType, trace);
        }

        private static bool ShouldMirror(LogType type)
        {
            return CommandAPI.UnityLogLevel switch
            {
                UnityLogLevel.Log => true,
                UnityLogLevel.Warning => type != LogType.Log,
                UnityLogLevel.Error => type == LogType.Error || type == LogType.Exception || type == LogType.Assert,
                _ => false
            };
        }

        #endregion
    }
}
