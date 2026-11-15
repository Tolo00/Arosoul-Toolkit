using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

        VisualElement root;
        int selectedTab = -1;

        public event Action CloseRequested;
        public bool IsVisible { get; private set; }


        sealed class CategoryState {
            public CategoryLayout Layout;
            public bool Instant;
            public readonly List<string> Ids = new List<string>();
        }

        static readonly string[] NoIds = new string[0];
        readonly List<CategoryState> states = new List<CategoryState>();
        Button applyButton, undoButton, resetButton;

        CategoryState CurrentState => selectedTab >= 0 && selectedTab < states.Count ? states[selectedTab] : null;
        IEnumerable<string> CurrentIds => CurrentState != null ? CurrentState.Ids : (IEnumerable<string>)NoIds; // null would mean "all"

        

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
            root.style.display = DisplayStyle.None;
            IsVisible = false;
        }

        public void Dispose() {
            store.StateChanged -= OnStateChanged;
            root?.RemoveFromHierarchy();
            root = null;
            bindings.Clear();
            tabs.Clear();
            pages.Clear();
            states.Clear();
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
                    tabButton.clicked += () => Select(index);
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
                if (!store.TryGetDefinition(entry.settingId, out var definition)) {
                    // Either removed from the project or not available on this platform.
                    if (!SettingsRegistry.TryGet(entry.settingId, out _)) {
                        Debug.LogWarning($"[Settings] Layout '{layout.name}' references unknown setting '{entry.settingId}'.");
                    }
                    continue;
                }

                var display = SettingDisplays.Find(entry.displayType);
                if (display == null || !display.Supports(definition.Type)) display = SettingDisplays.Default(definition.Type);
                if (display == null) continue;

                var asset = Template(theme.GetDisplayTemplate(display.Id), display.TemplateName);
                if (asset == null) continue;

                var row = asset.Instantiate();
                var label = row.Q<Label>("label");
                if (label != null) label.text = definition.DisplayName;

                var binding = new SettingBinding(store, definition, state.Instant);
                bindings.Add(binding);
                state.Ids.Add(definition.Id);
                display.Bind(row, binding);
                container.Add(row);
            }
        }

        void BuildFooter() {
            var footer = root.Q("footer");
            var buttonAsset = Template(theme.ButtonTemplate, "Button");
            if (footer == null || buttonAsset == null) return;

            // All three act on the selected category only. RefreshFooter shows/hides them per category.
            applyButton = AddButton(footer, buttonAsset, "Apply", "settings-button--apply", () => store.ApplyPending(CurrentIds));
            undoButton = AddButton(footer, buttonAsset, "Undo", "settings-button--undo", () => store.RevertPending(CurrentIds));
            resetButton = AddButton(footer, buttonAsset, "Reset", "settings-button--reset", () => {
                var state = CurrentState;
                if (state != null) store.ResetToDefaults(state.Ids, state.Instant);
            });

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
            bool pending = state != null && store.HasPendingIn(state.Ids);

            SetShown(applyButton, manual);
            SetShown(undoButton, manual);
            SetShown(resetButton, state != null && state.Layout.showResetButton);
            applyButton?.SetEnabled(pending);
            undoButton?.SetEnabled(pending);
        }

        static void SetShown(Button button, bool shown) {
            if (button != null) button.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

#endregion

}
