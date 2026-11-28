using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Commands
{
    /// <summary>
    /// The console's UI Toolkit elements: a drop-down panel with the log and the input field, and the suggestion list below it.
    /// Styled in code. Keys are handled by <see cref="CommandConsole"/>; this view only blocks their default UI behaviour.
    /// </summary>
    internal sealed class CommandConsoleView
    {
        private static readonly Color BackgroundColor = new(0.06f, 0.07f, 0.09f, 0.97f);
        private static readonly Color InputBackgroundColor = new(0.11f, 0.12f, 0.15f, 1f);
        private static readonly Color AccentColor = new(0.42f, 0.68f, 1f, 1f);
        private static readonly Color TextColor = new(0.87f, 0.89f, 0.92f, 1f);
        private static readonly Color DimTextColor = new(0.56f, 0.59f, 0.64f, 1f);
        private static readonly Color InputEchoColor = new(0.55f, 0.78f, 1f, 1f);
        private static readonly Color WarningColor = new(1f, 0.8f, 0.35f, 1f);
        private static readonly Color ErrorColor = new(1f, 0.45f, 0.42f, 1f);
        private static readonly Color ArgumentColor = new(0.72f, 0.82f, 0.6f, 1f);
        private static readonly Color SelectedColor = new(0.2f, 0.33f, 0.55f, 1f);

        private readonly CommandsSettingsData _settings;
        private readonly Func<bool> _isConsoleKeyHeld;
        private readonly VisualElement _root;
        private readonly ScrollView _log;
        private readonly TextField _input;
        private readonly VisualElement _suggestions;

        private bool _scrollToBottomPending;

        /// <param name="isConsoleKeyHeld">True while one of the console's keys is held, so the input field ignores it.</param>
        public CommandConsoleView(CommandsSettingsData settings, Func<bool> isConsoleKeyHeld)
        {
            _settings = settings;
            _isConsoleKeyHeld = isConsoleKeyHeld;

            _root = new VisualElement { name = "arosoul-commands-console", pickingMode = PickingMode.Ignore };
            _root.style.position = Position.Absolute;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.top = 0;
            _root.style.height = Length.Percent(Mathf.Clamp01(settings.Height) * 100f);
            _root.style.fontSize = settings.FontSize;
            _root.style.color = TextColor;
            _root.style.display = DisplayStyle.None;

            var panel = new VisualElement { name = "panel" };
            panel.style.flexGrow = 1;
            panel.style.backgroundColor = BackgroundColor;
            panel.style.borderBottomWidth = 2;
            panel.style.borderBottomColor = AccentColor;
            SetPadding(panel, 8, 10);
            _root.Add(panel);

            _log = new ScrollView(ScrollViewMode.Vertical) { name = "log" };
            _log.style.flexGrow = 1;
            _log.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _log.contentContainer.RegisterCallback<GeometryChangedEvent>(OnLogGeometryChanged);
            panel.Add(_log);

            panel.Add(CreateInputRow(out _input));

            _suggestions = new VisualElement { name = "suggestions" };
            _suggestions.style.position = Position.Absolute;
            _suggestions.style.top = Length.Percent(100);
            _suggestions.style.left = 10;
            _suggestions.style.maxWidth = Length.Percent(80);
            _suggestions.style.backgroundColor = InputBackgroundColor;
            _suggestions.style.borderBottomLeftRadius = 4;
            _suggestions.style.borderBottomRightRadius = 4;
            _suggestions.style.display = DisplayStyle.None;
            SetPadding(_suggestions, 4, 0);
            _root.Add(_suggestions);
        }

        public VisualElement Root => _root;
        public string InputText => _input.value ?? string.Empty;

        /// <summary>
        /// Raised when the user edits the input, not when the text is set from code.
        /// </summary>
        public event Action<string> OnInputChanged;

        /// <summary>
        /// Raised with the index of a clicked suggestion.
        /// </summary>
        public event Action<int> OnSuggestionClicked;

        public void SetOpen(bool open)
        {
            _root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open) _scrollToBottomPending = true;
        }

        public void FocusInput()
        {
            _input.schedule.Execute(() =>
            {
                _input.Focus();
                MoveCaretToEnd();
            });
        }

        public void BlurInput()
        {
            _input.Blur();
        }

        /// <param name="notify">Raise <see cref="OnInputChanged"/> as if the user typed the text.</param>
        public void SetInput(string text, bool notify)
        {
            _input.SetValueWithoutNotify(text);
            MoveCaretToEnd();
            _input.schedule.Execute(MoveCaretToEnd);
            if (notify) OnInputChanged?.Invoke(text);
        }

        #region Log

        public void AddEntry(CommandLogEntry entry)
        {
            var container = new VisualElement();
            string text = entry.Type == CommandLogType.Input ? $"> {entry.Message}" : entry.Message;
            Label label = CreateText(text, GetColor(entry.Type));
            container.Add(label);

            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                Label trace = CreateText(entry.StackTrace.TrimEnd(), DimTextColor);
                trace.style.display = DisplayStyle.None;
                trace.style.marginLeft = 16;
                trace.style.fontSize = Mathf.Max(8, _settings.FontSize - 2);
                container.Add(trace);

                bool expanded = false;
                label.text += "  [+]";
                label.RegisterCallback<ClickEvent>(_ =>
                {
                    expanded = !expanded;
                    trace.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            _log.Add(container);
            while (_log.contentContainer.childCount > _settings.MaxLogEntries)
                _log.contentContainer.RemoveAt(0);

            _scrollToBottomPending = true;
        }

        public void ClearEntries()
        {
            _log.Clear();
        }

        #endregion

        #region Suggestions

        /// <param name="selectable">False shows the commands as usage hints, without a selection.</param>
        public void ShowSuggestions(IReadOnlyList<CommandInfo> commands, bool selectable)
        {
            _suggestions.Clear();
            if (commands.Count == 0)
            {
                _suggestions.style.display = DisplayStyle.None;
                return;
            }

            for (int i = 0; i < commands.Count; i++)
                _suggestions.Add(CreateSuggestionRow(commands[i], i, selectable));

            _suggestions.style.display = DisplayStyle.Flex;
            SetSelectedSuggestion(selectable ? 0 : -1);
        }

        public void SetSelectedSuggestion(int index)
        {
            for (int i = 0; i < _suggestions.childCount; i++)
                _suggestions[i].style.backgroundColor = i == index ? SelectedColor : Color.clear;
        }

        private VisualElement CreateSuggestionRow(CommandInfo command, int index, bool selectable)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            SetPadding(row, 2, 10);

            Label name = CreateText(command.Name, selectable ? AccentColor : TextColor);
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.whiteSpace = WhiteSpace.NoWrap;
            row.Add(name);

            if (command.ArgumentSignature.Length > 0)
            {
                Label arguments = CreateText(command.ArgumentSignature, ArgumentColor);
                arguments.style.marginLeft = 8;
                arguments.style.whiteSpace = WhiteSpace.NoWrap;
                row.Add(arguments);
            }

            if (command.Description.Length > 0)
            {
                Label description = CreateText(command.Description, DimTextColor);
                description.style.marginLeft = 12;
                description.style.flexShrink = 1;
                description.style.whiteSpace = WhiteSpace.NoWrap;
                description.style.overflow = Overflow.Hidden;
                description.style.textOverflow = TextOverflow.Ellipsis;
                row.Add(description);
            }

            if (selectable)
            {
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    OnSuggestionClicked?.Invoke(index);
                    FocusInput();
                });
            }
            return row;
        }

        #endregion

        #region Input

        private VisualElement CreateInputRow(out TextField input)
        {
            var row = new VisualElement { name = "input-row" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 6;
            row.style.backgroundColor = InputBackgroundColor;
            row.style.borderTopLeftRadius = 4;
            row.style.borderTopRightRadius = 4;
            row.style.borderBottomLeftRadius = 4;
            row.style.borderBottomRightRadius = 4;
            SetPadding(row, 4, 8);

            Label prompt = CreateText(">", AccentColor);
            prompt.style.unityFontStyleAndWeight = FontStyle.Bold;
            prompt.style.marginRight = 6;
            row.Add(prompt);

            input = new TextField { name = "input" };
            input.style.flexGrow = 1;
            input.style.marginLeft = 0;
            input.style.marginRight = 0;
            input.style.marginTop = 0;
            input.style.marginBottom = 0;

            VisualElement textInput = input.Q(className: TextField.inputUssClassName);
            if (textInput != null)
            {
                textInput.style.backgroundColor = Color.clear;
                textInput.style.borderLeftWidth = 0;
                textInput.style.borderRightWidth = 0;
                textInput.style.borderTopWidth = 0;
                textInput.style.borderBottomWidth = 0;
                textInput.style.color = TextColor;
                textInput.style.fontSize = _settings.FontSize;
                SetPadding(textInput, 0, 0);
            }

            input.RegisterValueChangedCallback(e => OnInputChanged?.Invoke(e.newValue));
            input.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
            input.RegisterCallback<NavigationMoveEvent>(BlockEvent, TrickleDown.TrickleDown);
            input.RegisterCallback<NavigationSubmitEvent>(BlockEvent, TrickleDown.TrickleDown);
            input.RegisterCallback<NavigationCancelEvent>(BlockEvent, TrickleDown.TrickleDown);
            row.Add(input);
            return row;
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            // Console keys (Tab, arrows, Enter, the toggle key) must not move focus, the caret or type characters
            if (_isConsoleKeyHeld()) BlockEvent(evt);
        }

        private void BlockEvent(EventBase evt)
        {
            evt.StopImmediatePropagation();
            _input.focusController?.IgnoreEvent(evt);
        }

        private void MoveCaretToEnd()
        {
            int end = InputText.Length;
            _input.SelectRange(end, end);
        }

        #endregion

        #region Helpers

        private void OnLogGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_scrollToBottomPending) return;

            _scrollToBottomPending = false;
            _log.scrollOffset = new Vector2(0f, Mathf.Max(0f, _log.contentContainer.layout.height - _log.contentViewport.layout.height));
        }

        private static Label CreateText(string text, Color color)
        {
            var label = new Label(text) { enableRichText = false };
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;
            label.style.paddingTop = 1;
            label.style.paddingBottom = 1;
            label.style.paddingLeft = 0;
            label.style.paddingRight = 0;
            label.selection.isSelectable = true;
            return label;
        }

        private static Color GetColor(CommandLogType type)
        {
            return type switch
            {
                CommandLogType.Input => InputEchoColor,
                CommandLogType.Warning => WarningColor,
                CommandLogType.Error => ErrorColor,
                _ => TextColor
            };
        }

        private static void SetPadding(VisualElement element, float vertical, float horizontal)
        {
            element.style.paddingTop = vertical;
            element.style.paddingBottom = vertical;
            element.style.paddingLeft = horizontal;
            element.style.paddingRight = horizontal;
        }

        #endregion
    }
}
