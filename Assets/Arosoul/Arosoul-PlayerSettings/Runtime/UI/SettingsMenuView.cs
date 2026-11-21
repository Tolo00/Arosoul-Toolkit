using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Dir = UnityEngine.UIElements.NavigationMoveEvent.Direction;

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
        bool navActive;

        VisualElement root;
        Button applyButton, undoButton, resetButton;
        VisualElement footer, focusedRow, focusBeforePrompt;
        Vector2 lastPointer;

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
            public VisualElement LastFocus;
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

            host.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            host.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            root.RegisterCallback<FocusInEvent>(OnFocusIn);
            root.RegisterCallback<FocusOutEvent>(OnFocusOut);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            host.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(e => { if (e.pointerType == UnityEngine.UIElements.PointerType.mouse) SetNavActive(false); }, TrickleDown.TrickleDown);

            Select(0);
            OnStateChanged();
            Hide();
        }

        public void Show() {
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
            IsVisible = true;
            OnStateChanged();
            SetNavActive(true);
            root.schedule.Execute(FocusSelectedTab);
        }

        public void Hide() {
            DropdownPopup.Close(false);

            if (root == null) return;
            ClosePrompt();
            root.style.display = DisplayStyle.None;
            IsVisible = false;
        }

        public void Dispose() {
            store.StateChanged -= OnStateChanged;
            foreach (var section in allSections) section.StateChanged -= OnStateChanged;

            host.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            host.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            host.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);

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
                    tabButton.clicked += () => RequestTab(index);
                    tabButton.RegisterCallback<FocusInEvent>(_ => RequestTab(index)); // Focus determines the shown tab.
                    tabButton.RegisterCallback<NavigationSubmitEvent>(_ => FocusContent()); // South on a tab enters the page.
                    tabs.Add(tabButton);
                }
                tabContainer.Add(tabElement);

                var page = categoryAsset.Instantiate();
                page.Query<ScrollView>().ForEach(s => s.verticalScrollerVisibility = ScrollerVisibility.Hidden);
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
            SettingsNavigation.MarkRow(row);
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
            footer = root.Q("footer");
            var buttonAsset = Template(theme.ButtonTemplate, "Button");
            if (footer == null || buttonAsset == null) return;

            // All three act on the selected category only. RefreshFooter shows/hides them per category.
            applyButton = AddButton(footer, buttonAsset, "Apply", "settings-button--apply", () => { ApplyScope(CurrentState); if (navActive) FocusTopOfContent(); });
            undoButton = AddButton(footer, buttonAsset, "Undo", "settings-button--undo", () => { RevertScope(CurrentState); if (navActive) FocusTopOfContent(); });

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

        void FocusTopOfContent() {
            var state = CurrentState;
            if (state == null) return;
            (Navigables(pages[selectedTab]).FirstOrDefault() ?? Navigables(footer).FirstOrDefault())?.Focus();
        }

#endregion

#region Runtime

        void Select(int index) {
            DropdownPopup.Close(false);

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
            DropdownPopup.Close(true);
            focusBeforePrompt = FocusedElement;
            promptOverlay.Q<Button>("save").Focus();
        }

        /// <summary>Hides the prompt without applying or discarding anything.</summary>
        public void DismissPrompt() {
            if (!IsPromptOpen) return;
            ClosePrompt();
            RestoreFocus();
        }

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

#endregion


#region Navigation

        VisualElement FocusedElement => root?.panel?.focusController?.focusedElement as VisualElement;

        /// <summary>Called every frame by the host for input UI Toolkit doesn't turn into navigation events.</summary>
        public void Tick() {
            if (!IsVisible || IsPromptOpen || SettingsNavigation.IsBlocked) return;
            int direction = MenuInput.TabDirection();
            if (direction == 0) return;
            SetNavActive(true);
            RequestTab(selectedTab + direction);
        }

        // Switches page. Asks about unapplied changes first. Focus follows: stays on the tab bar, or lands in the new page.
        void RequestTab(int index) {
            if (index < 0 || index >= pages.Count || index == selectedTab || IsPromptOpen) return;

            var focused = FocusedElement;
            bool onTab = focused == null || (focused is Button b && tabs.Contains(b));
            void Go() {
                Select(index);
                if (onTab) tabs[index].Focus();
                else FocusContent();
            }

            var state = CurrentState;
            if (state != null && HasPendingIn(state)) ShowPrompt(state, Go);
            else Go();
        }

        void SetNavActive(bool active) {
            if (navActive == active || root == null) return;
            navActive = active;
            root.EnableInClassList(SettingsNavigation.NavActiveClass, active);
        }

        bool Owns(IEventHandler target) =>
            IsVisible && root != null && target is VisualElement ve && (ve == host || ve == root || root.Contains(ve));

        static bool IsNavigable(VisualElement e) =>
            e.focusable && e.canGrabFocus && !e.delegatesFocus && e.tabIndex >= 0
            && !(e is ScrollView) && !(e is Scroller) && e.GetFirstAncestorOfType<Scroller>() == null; // canGrabFocus is false for disabled/hidden elements

        static List<VisualElement> Navigables(VisualElement scope) {
            var list = new List<VisualElement>();
            scope?.Query<VisualElement>().ForEach(e => { if (IsNavigable(e)) list.Add(e); });
            return list;
        }

        static VisualElement FindRow(VisualElement e) {
            for (; e != null; e = e.parent) if (e.ClassListContains(SettingsNavigation.RowClass)) return e;
            return null;
        }

        void FocusSelectedTab() {
            if (selectedTab >= 0 && selectedTab < tabs.Count) tabs[selectedTab].Focus();
        }

        void FocusContent() {
            var state = CurrentState;
            if (state == null) return;

            var list = Navigables(pages[selectedTab]);
            var target = state.LastFocus != null && list.Contains(state.LastFocus) ? state.LastFocus : list.FirstOrDefault();
            if (target == null) target = Navigables(footer).FirstOrDefault(); // empty page
            target?.Focus();
        }

        void FocusFooter(VisualElement from) {
            var list = Navigables(footer);
            if (list.Count == 0) return;
            float x = from.worldBound.center.x;
            list.OrderBy(e => Mathf.Abs(e.worldBound.center.x - x)).First().Focus();
        }

        void RestoreFocus() {
            var target = focusBeforePrompt;
            focusBeforePrompt = null;
            int tab = target is Button b ? tabs.IndexOf(b) : -1;
            if (target == null || target.panel == null || !target.canGrabFocus || (tab >= 0 && tab != selectedTab)) FocusSelectedTab();
            else target.Focus();
        }

        void OnNavigationMove(NavigationMoveEvent evt) {
            if (!Owns(evt.target)) return;

            var dir = evt.direction;
            if (dir != Dir.Left && dir != Dir.Right && dir != Dir.Up && dir != Dir.Down) return; // Tab / Shift+Tab keep Unity's default
            SettingsNavigation.Consume(evt);
            if (SettingsNavigation.IsBlocked) return;

            var current = evt.target as VisualElement;

            // First key/stick press after using the mouse only reveals the highlight (last hovered row, else the top one).
            if (!navActive) {
                SetNavActive(true);
                if (!IsPromptOpen && !DropdownPopup.IsOpen) { FocusContent(); return; }
            }

            if (current == host || current == root) { FocusContent(); return; } // nothing was focused (e.g. a button got disabled)

            if (DropdownPopup.Contains(current)) {
                if (dir == Dir.Up || dir == Dir.Down) Spatial(current, dir, Navigables(DropdownPopup.Overlay))?.Focus();
                return;
            }

            if (IsPromptOpen) {
                Spatial(current, dir, Navigables(promptOverlay))?.Focus();
                return;
            }

            if (current is Button button && tabs.Contains(button)) { MoveOnTabs(tabs.IndexOf(button), dir); return; }

            if (selectedTab >= 0 && pages[selectedTab].Contains(current)) { MoveInContent(current, dir); return; }

            if (footer != null && footer.Contains(current)) {
                if (dir == Dir.Up) FocusContent();
                else Spatial(current, dir, Navigables(footer))?.Focus();
            }
        }

        // Runs before the focused element sees Submit, so the first press after using the mouse doesn't activate something invisible.
        void OnNavigationSubmit(NavigationSubmitEvent evt) {
            if (!Owns(evt.target) || SettingsNavigation.IsBlocked) return;

            bool wasActive = navActive;
            SetNavActive(true);
            if (wasActive || IsPromptOpen || DropdownPopup.IsOpen) return;

            FocusContent();
            SettingsNavigation.Consume(evt);
        }

        /// East / Escape: 
        ///     Prompt: dismiss
        ///     Content/footer: back to the tabs (asking about unapplied changes)
        ///     Tabs close.
        void OnNavigationCancel(NavigationCancelEvent evt) {
            if (!Owns(evt.target)) return;
            SettingsNavigation.Consume(evt);
            if (SettingsNavigation.IsBlocked) return;
            SetNavActive(true);

            if (DropdownPopup.IsOpen) { DropdownPopup.Close(true); return; }
            if (IsPromptOpen) { DismissPrompt(); return; }

            var current = evt.target as VisualElement;
            bool inside = current != null && ((selectedTab >= 0 && pages[selectedTab].Contains(current)) || (footer != null && footer.Contains(current)));
            if (!inside) { CloseRequested?.Invoke(); return; }

            var state = CurrentState;
            if (state != null && HasPendingIn(state)) ShowPrompt(state, FocusSelectedTab);
            else FocusSelectedTab();
        }

        void MoveOnTabs(int index, Dir dir) {
            bool horizontal = layout.TabOrientation == TabOrientation.Horizontal;
            if (dir == (horizontal ? Dir.Left : Dir.Up)) RequestTab(index - 1);
            else if (dir == (horizontal ? Dir.Right : Dir.Down)) RequestTab(index + 1);
        }

        void MoveInContent(VisualElement current, Dir dir) {
            var list = Navigables(pages[selectedTab]);
            if (!list.Contains(current)) current = list.FirstOrDefault(e => e.Contains(current));
            if (current == null) { FocusContent(); return; } // e.g. the scroll view itself had focus

            var next = Spatial(current, dir, list);
            if (next != null) next.Focus();
            else if (dir == Dir.Down) FocusFooter(current);
        }

        // Nearest candidate in a direction. Up/Down prefer the next row, then the closest column; Left/Right stay in the row.
        static VisualElement Spatial(VisualElement from, Dir dir, List<VisualElement> candidates) {
            float tolerance = Mathf.Max(4f, from.worldBound.height * 0.5f);
            bool vertical = dir == Dir.Up || dir == Dir.Down;

            float minPrimary = float.MaxValue;
            foreach (var c in candidates) {
                if (c == from || c.Contains(from) || from.Contains(c)) continue;
                if (Measure(from, c, dir, tolerance, out float primary, out _)) minPrimary = Mathf.Min(minPrimary, primary);
            }
            if (minPrimary == float.MaxValue) return null;

            VisualElement best = null;
            float bestSecondary = float.MaxValue;
            float pool = vertical ? tolerance : 0f;
            foreach (var c in candidates) {
                if (c == from || c.Contains(from) || from.Contains(c)) continue;
                if (Measure(from, c, dir, tolerance, out float primary, out float secondary) && primary <= minPrimary + pool && secondary < bestSecondary) {
                    best = c;
                    bestSecondary = secondary;
                }
            }
            return best;
        }

        static bool Measure(VisualElement from, VisualElement to, Dir dir, float tolerance, out float primary, out float secondary) {
            var a = from.worldBound.center;
            var b = to.worldBound.center;
            float dx = b.x - a.x, dy = b.y - a.y;
            switch (dir) {
                case Dir.Left:  primary = -dx; secondary = Mathf.Abs(dy); return dx < -1f && Mathf.Abs(dy) <= tolerance;
                case Dir.Right: primary = dx;  secondary = Mathf.Abs(dy); return dx > 1f && Mathf.Abs(dy) <= tolerance;
                case Dir.Up:    primary = -dy; secondary = Mathf.Abs(dx); return dy < -tolerance;
                case Dir.Down:  primary = dy;  secondary = Mathf.Abs(dx); return dy > tolerance;
                default:        primary = secondary = 0f; return false;
            }
        }

        void OnFocusIn(FocusInEvent evt) {
            var target = evt.target as VisualElement;
            if (target == null) return;

            SetFocusedRow(FindRow(target));

            for (int i = 0; i < pages.Count; i++) {
                if (pages[i].Contains(target)) states[i].LastFocus = target;
            }

            var scroll = target.GetFirstAncestorOfType<ScrollView>();
            if (scroll != null && scroll.contentContainer.Contains(target)) scroll.ScrollTo(target);
        }

        void OnFocusOut(FocusOutEvent evt) => SetFocusedRow(null);

        void SetFocusedRow(VisualElement row) {
            if (row == focusedRow) return;
            focusedRow?.RemoveFromClassList(SettingsNavigation.FocusedRowClass);
            focusedRow = row;
            focusedRow?.AddToClassList(SettingsNavigation.FocusedRowClass);
        }

        // Hovering moves focus only when the mouse really moved. Scrolling or layout changes under a still mouse don't.
        void OnPointerMove(PointerMoveEvent evt) {
            if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse || evt.pressedButtons != 0) return;

            Vector2 position = evt.position;
            if ((position - lastPointer).sqrMagnitude < 1f) return;
            lastPointer = position;

            SetNavActive(false); // real mouse movement: hide the keyboard/controller highlight

            if (IsPromptOpen || SettingsNavigation.IsBlocked) return;

            var target = evt.target as VisualElement;
            if (target == null) return;

            var focused = FocusedElement;
            if (focused != null && (focused is TextField || focused.GetFirstAncestorOfType<TextField>() != null)) return;

            VisualElement hovered = null;
            for (var e = target; e != null && e != root; e = e.parent) {
                if (IsNavigable(e)) { hovered = e; break; }
            }
            if (hovered == null) hovered = Navigables(FindRow(target)).FirstOrDefault(); // over a label: use the row's control

            if (hovered == null || hovered == focused) return;
            if (hovered is Button b && tabs.Contains(b)) return; // focusing a tab switches page, so tabs only highlight on hover

            hovered.Focus();
        }

#endregion

    }
}