using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Builds the settings UI from a layout asset and UXML templates, and keeps it in sync with the store.
    /// Plain class so it can be hosted by any VisualElement (SettingsMenu is just the default host).    
    /// </summary>
    public sealed class SettingsMenuView : IDisposable {
        const string ResourcePath = "PlayerSettings/";

        readonly VisualElement host;
        readonly SettingsLayout layout;
        readonly SettingsStore store;
        readonly SettingsTheme theme;
        readonly bool ownsTheme;

        readonly Dictionary<string, VisualTreeAsset> templateCache = new Dictionary<string, VisualTreeAsset>();
        readonly List<SettingBinding> bindings = new List<SettingBinding>();
        readonly List<Button> tabs = new List<Button>();
        readonly List<VisualElement> pages = new List<VisualElement>();
        readonly List<CategoryState> states = new List<CategoryState>();
        readonly List<SettingSection> allSections = new List<SettingSection>();

        int selectedTab = -1;

        VisualElement root;
        Button applyButton, undoButton, resetButton;

        VisualElement window, promptOverlay;
        CategoryState promptScope;
        Action promptResolved;


        public event Action CloseRequested;
        public bool IsVisible { get; private set; }
        public bool IsPromptOpen { get; private set; }

        /// <summary>Any category has changes that are not applied yet (settings or sections).</summary>
        public bool HasPending => store.HasPending || allSections.Any(s => s.HasPending);

        CategoryState CurrentState => selectedTab >= 0 && selectedTab < states.Count ? states[selectedTab] : null;


        sealed class CategoryState {
            public CategoryLayout Layout;
            public bool Instant;
            public readonly List<string> Ids = new List<string>();
            public readonly List<KeyValuePair<SettingSection, string>> Sections = new List<KeyValuePair<SettingSection, string>>();
        }
        
        

        public SettingsMenuView(VisualElement host, SettingsLayout layout, SettingsStore store) {
            this.host = host;
            this.layout = layout;
            this.store = store;

            theme = layout.Theme;
            if (theme == null) {
                theme = ScriptableObject.CreateInstance<SettingsTheme>(); // empty theme => every template falls back to the defaults
                ownsTheme = true;
            }
        }

        public void Build() {
            var menuAsset = Template(theme.MenuTemplate, "Menu");
            if (menuAsset == null) return;

            root = menuAsset.Instantiate();
            root.AddToClassList("settings-root");
            root.AddToClassList(layout.TabOrientation == TabOrientation.Vertical ? "settings-tabs-vertical" : "settings-tabs-horizontal");
            ApplyStyles();

            var title = root.Q<Label>("title");
            if (title != null) title.text = layout.Title;

            BuildCategories();
            BuildFooter();

            window = root.Q("window");
            BuildPrompt();

            store.StateChanged += OnStateChanged;
            host.Add(root);

            Select(0);
            OnStateChanged();
            Hide();
        }

        public void Show() {
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
            IsVisible = true;
            OnStateChanged();
        }

        public void Hide() {
            if (root == null) return;
            ClosePrompt();
            root.style.display = DisplayStyle.None;
            IsVisible = false;
        }

        public void Dispose() {
            store.StateChanged -= OnStateChanged;
            foreach (var section in allSections) section.StateChanged -= OnStateChanged;

            root?.RemoveFromHierarchy();
            root = null;
            bindings.Clear();
            tabs.Clear();
            pages.Clear();
            states.Clear();
            allSections.Clear();

            if (ownsTheme && theme != null) UnityEngine.Object.Destroy(theme);
        }

#region Construction

        void ApplyStyles() {
            if (theme.IncludeDefaultStyle) {
                var defaultStyle = Resources.Load<StyleSheet>(ResourcePath + "PlayerSettingsDefault");
                if (defaultStyle != null) root.styleSheets.Add(defaultStyle);
            }
            foreach (var sheet in theme.StyleSheets) {
                if (sheet != null) root.styleSheets.Add(sheet);
            }
        }

        void BuildCategories() {
            var tabContainer = root.Q("tabs");
            var pageContainer = root.Q("pages");
            var tabAsset = Template(theme.TabTemplate, "Tab");
            var categoryAsset = Template(theme.CategoryTemplate, "Category");
            if (tabContainer == null || pageContainer == null || tabAsset == null || categoryAsset == null) return;

            for (int i = 0; i < layout.Categories.Count; i++) {
                var category = layout.Categories[i];
                int index = i;

                var tabElement = tabAsset.Instantiate();
                var tabButton = tabElement.Q<Button>("tab");
                if (tabButton != null) {
                    tabButton.text = category.name;
                    tabButton.clicked += () => TrySelect(index);
                    tabs.Add(tabButton);
                }
                tabContainer.Add(tabElement);

                var page = categoryAsset.Instantiate();
                page.style.flexGrow = 1;
                var entries = page.Q("entries") ?? page;
                var state = new CategoryState { Layout = category, Instant = category.applyMode == ApplyMode.Instant };
                states.Add(state);
                BuildEntries(state, entries);
                pageContainer.Add(page);
                pages.Add(page);
            }
        }

        void BuildEntries(CategoryState state, VisualElement container) {
            foreach (var entry in state.Layout.entries) {
                if (store.TryGetDefinition(entry.settingId, out var definition)) {
                    BuildSettingRow(state, entry, definition, container);
                    continue;
                }

                if (SettingsRegistry.TryGetSection(entry.settingId, out var section)) {
                    BuildSection(state, entry, section, container);
                    continue;
                }

                if (SettingsRegistry.TryGetDecoration(entry.settingId, out var decoration)) {
                    var element = decoration.Build(new DecorationContext(entry, decoration.Parameters));
                    if (element != null) container.Add(element);
                    continue;
                }

                // Either removed from the project or not available on this platform.
                if (!SettingsRegistry.TryGet(entry.settingId, out _)) {
                    Debug.LogWarning($"[Settings] Layout '{layout.name}' references unknown setting '{entry.settingId}'.");
                }
            }
        }

        void BuildSettingRow(CategoryState state, EntryLayout entry, SettingDefinition definition, VisualElement container) {
            var display = SettingDisplays.Find(entry.displayType);
            if (display == null || !display.Supports(definition.Type)) display = SettingDisplays.Default(definition.Type);
            if (display == null) return;

            var asset = Template(theme.GetDisplayTemplate(display.Id), display.TemplateName);
            if (asset == null) return;

            var row = asset.Instantiate();
            var label = row.Q<Label>("label");
            if (label != null) label.text = definition.DisplayName;

            var binding = new SettingBinding(store, definition, state.Instant);
            bindings.Add(binding);
            if (!state.Ids.Contains(definition.Id)) state.Ids.Add(definition.Id);
            display.Bind(row, binding);
            container.Add(row);
        }

        void BuildSection(CategoryState state, EntryLayout entry, SettingSection section, VisualElement container) {
            string displayId = section.Displays.Any(d => d.Id == entry.displayType) ? entry.displayType : section.Displays.FirstOrDefault()?.Id;
            if (displayId == null) return;

            var context = new SectionContext(displayId, state.Instant, theme.IncludeDefaultStyle,
                name => Template(theme.GetDisplayTemplate(name), name));

            var element = section.Build(context);
            if (element == null) return;

            container.Add(element);
            state.Sections.Add(new KeyValuePair<SettingSection, string>(section, displayId));

            if (!allSections.Contains(section)) {
                allSections.Add(section);
                section.StateChanged += OnStateChanged;
            }
        }

        void BuildFooter() {
            var footer = root.Q("footer");
            var buttonAsset = Template(theme.ButtonTemplate, "Button");
            if (footer == null || buttonAsset == null) return;

            // All three act on the selected category only. RefreshFooter shows/hides them per category.
            applyButton = AddButton(footer, buttonAsset, "Apply", "settings-button--apply", () => ApplyScope(CurrentState));
            undoButton = AddButton(footer, buttonAsset, "Undo", "settings-button--undo", () => RevertScope(CurrentState));
            resetButton = AddButton(footer, buttonAsset, "Reset", "settings-button--reset", () => ResetScope(CurrentState));

            if (layout.ShowCloseButton) {
                AddButton(footer, buttonAsset, "Close", "settings-button--close", () => CloseRequested?.Invoke());
            }
        }

        static Button AddButton(VisualElement parent, VisualTreeAsset asset, string text, string modifierClass, Action onClick) {
            var element = asset.Instantiate();
            var button = element.Q<Button>("button");
            if (button == null) {
                Debug.LogError("[Settings] Template 'Button' needs a Button named 'button'.");
                return null;
            }

            button.text = text;
            button.AddToClassList(modifierClass);
            button.clicked += onClick;
            parent.Add(element);
            return button;
        }

#endregion

#region Runtime

        void Select(int index) {
            if (pages.Count == 0) return;
            selectedTab = Mathf.Clamp(index, 0, pages.Count - 1);

            for (int i = 0; i < pages.Count; i++) {
                pages[i].style.display = i == selectedTab ? DisplayStyle.Flex : DisplayStyle.None;
            }
            for (int i = 0; i < tabs.Count; i++) {
                tabs[i].EnableInClassList("settings-tab--selected", i == selectedTab);
            }
            RefreshFooter();
        }

        void OnStateChanged() {
            foreach (var binding in bindings) binding.NotifyValueChanged();
            RefreshFooter();
        }

        VisualTreeAsset Template(VisualTreeAsset overrideAsset, string resourceName) {
            if (overrideAsset != null) return overrideAsset;

            if (!templateCache.TryGetValue(resourceName, out var asset)) {
                asset = Resources.Load<VisualTreeAsset>(ResourcePath + resourceName);
                templateCache[resourceName] = asset;
                if (asset == null) Debug.LogError($"[Settings] Missing UXML template 'Resources/{ResourcePath}{resourceName}'.");
            }
            return asset;
        }

        void RefreshFooter() {
            var state = CurrentState;
            bool manual = state != null && !state.Instant;
            bool pending = state != null && HasPendingIn(state);

            SetShown(applyButton, manual);
            SetShown(undoButton, manual);
            SetShown(resetButton, state != null && state.Layout.showResetButton);
            applyButton?.SetEnabled(pending);
            undoButton?.SetEnabled(pending);
        }

        static void SetShown(Button button, bool shown) {
            if (button != null) button.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // A null scope means every category.
        bool HasPendingIn(CategoryState scope) {
            if (scope == null) return HasPending;
            return store.HasPendingIn(scope.Ids) || scope.Sections.Any(s => s.Key.HasPending);
        }

        void ApplyScope(CategoryState scope) {
            if (scope == null) {
                store.ApplyPending();
                foreach (var section in allSections) section.ApplyPending();
                return;
            }
            store.ApplyPending(scope.Ids);
            foreach (var pair in scope.Sections) pair.Key.ApplyPending();
        }

        void RevertScope(CategoryState scope) {
            if (scope == null) {
                store.RevertPending();
                foreach (var section in allSections) section.RevertPending();
                return;
            }
            store.RevertPending(scope.Ids);
            foreach (var pair in scope.Sections) pair.Key.RevertPending();
        }

        void ResetScope(CategoryState scope) {
            if (scope == null) return;
            store.ResetToDefaults(scope.Ids, scope.Instant);
            foreach (var pair in scope.Sections) pair.Key.ResetToDefaults(pair.Value, scope.Instant);
        }

#endregion

#region Unapplied changes prompt

        void BuildPrompt() {
            var asset = Template(theme.PromptTemplate, "Prompt");
            if (asset == null) return;

            var overlay = asset.Instantiate();
            var save = overlay.Q<Button>("save");
            var discard = overlay.Q<Button>("discard");
            if (save == null || discard == null) {
                Debug.LogError("[Settings] Template 'Prompt' needs Buttons named 'save' and 'discard'.");
                return;
            }

            // The template container has no size of its own, make it cover the whole menu.
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.display = DisplayStyle.None;

            save.clicked += () => ResolvePrompt(true);
            discard.clicked += () => ResolvePrompt(false);

            promptOverlay = overlay;
            root.Add(promptOverlay); // sibling of the window, so disabling the window doesn't disable the prompt
        }

        /// <summary>Asks whether to apply or discard ALL pending changes, then calls onResolved.</summary>
        public void ShowPrompt(Action onResolved) => ShowPrompt(null, onResolved);

        void ShowPrompt(CategoryState scope, Action onResolved) {
            if (promptOverlay == null) { // no prompt template available: discard silently
                RevertScope(scope);
                onResolved?.Invoke();
                return;
            }

            promptScope = scope;
            promptResolved = onResolved;
            IsPromptOpen = true;

            promptOverlay.style.display = DisplayStyle.Flex;
            window?.SetEnabled(false);
            promptOverlay.Q<Button>("save").Focus();
        }

        /// <summary>Hides the prompt without applying or discarding anything.</summary>
        public void DismissPrompt() => ClosePrompt();

        void ResolvePrompt(bool apply) {
            var scope = promptScope;
            var next = promptResolved;
            ClosePrompt();

            if (apply) ApplyScope(scope);
            else RevertScope(scope);
            next?.Invoke();
        }

        void ClosePrompt() {
            if (!IsPromptOpen) return;
            IsPromptOpen = false;
            promptScope = null;
            promptResolved = null;
            promptOverlay.style.display = DisplayStyle.None;
            window?.SetEnabled(true);
        }

        void TrySelect(int index) {
            if (index == selectedTab || IsPromptOpen) return;

            var state = CurrentState;
            if (state != null && HasPendingIn(state)) ShowPrompt(state, () => Select(index));
            else Select(index);
        }

#endregion

    }
}