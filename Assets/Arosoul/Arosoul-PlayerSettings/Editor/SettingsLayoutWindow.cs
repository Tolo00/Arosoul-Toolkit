using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings.EditorTools {

    /// <summary>
    /// Edits the categories of a SettingsLayout. Left: the categories (tabs). Right: every available setting,
    /// grouped and searchable, with a toggle (include in this category) and a dropdown (how it is displayed).
    /// </summary>
    public sealed class SettingsLayoutWindow : EditorWindow {
        const string MissingGroup = "Missing (no longer exist)";

        [SerializeField] SettingsLayout layout;

        int selected = -1;
        string search = string.Empty;
        IReadOnlyList<SettingDefinition> catalog = new List<SettingDefinition>();
        ListView categoryList;
        VisualElement rightPane;

        public static void Open(SettingsLayout layout) {
            var window = GetWindow<SettingsLayoutWindow>();
            window.titleContent = new GUIContent("Settings Layout");
            window.minSize = new Vector2(640, 300);
            window.layout = layout;
            window.selected = layout != null && layout.Categories.Count > 0 ? 0 : -1;
            window.Rebuild();
        }

        void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
        void CreateGUI() => Rebuild();

        void OnUndoRedo() {
            if (categoryList == null) return;
            categoryList.RefreshItems();
            BuildRightPane();
        }

        void Rebuild() {
            var root = rootVisualElement;
            root.Clear();

            if (layout == null) {
                root.Add(new Label("Select a Settings Layout asset and press 'Edit Categories' in its inspector."));
                return;
            }

            SettingsRegistry.Rebuild();
            catalog = SettingsRegistry.All;

            var split = new TwoPaneSplitView(0, 220, TwoPaneSplitViewOrientation.Horizontal);
            split.Add(BuildLeftPane());
            rightPane = new VisualElement();
            rightPane.style.flexGrow = 1;
            rightPane.style.paddingLeft = rightPane.style.paddingRight = rightPane.style.paddingTop = 6;
            split.Add(rightPane);
            root.Add(split);
            split.style.flexGrow = 1;

            if (selected >= layout.Categories.Count) selected = layout.Categories.Count - 1;
            if (selected >= 0) categoryList.SetSelectionWithoutNotify(new[] { selected });
            BuildRightPane();
        }

#region  Left: categories

        VisualElement BuildLeftPane() {
            var pane = new VisualElement();
            pane.style.minWidth = 160;
            pane.style.paddingLeft = pane.style.paddingRight = pane.style.paddingTop = 6;

            var header = new Label("Categories");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16f;
            header.style.marginBottom = 4;
            pane.Add(header);

            categoryList = new ListView {
                itemsSource = layout.Categories,
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                reorderable = true,
                makeItem = () => new Label(),
                bindItem = (e, i) => {
                    string name = layout.Categories[i].name;
                    ((Label)e).text = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name;
                }
            };
            categoryList.style.flexGrow = 1;
            categoryList.selectionChanged += _ => { selected = categoryList.selectedIndex; BuildRightPane(); };
            categoryList.itemIndexChanged += (from, to) => { EditorUtility.SetDirty(layout); selected = to; BuildRightPane(); };
            pane.Add(categoryList);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 4;
            buttons.Add(new Button(AddCategory) { text = "Add" });
            buttons.Add(new Button(RemoveCategory) { text = "Remove" });
            pane.Add(buttons);
            return pane;
        }

        void AddCategory() {
            Undo.RecordObject(layout, "Add Settings Category");
            layout.Categories.Add(new CategoryLayout { name = "Category " + (layout.Categories.Count + 1) });
            EditorUtility.SetDirty(layout);

            categoryList.RefreshItems();
            categoryList.SetSelection(layout.Categories.Count - 1);
        }

        void RemoveCategory() {
            if (selected < 0 || selected >= layout.Categories.Count) return;

            Undo.RecordObject(layout, "Remove Settings Category");
            layout.Categories.RemoveAt(selected);
            EditorUtility.SetDirty(layout);

            selected = Mathf.Min(selected, layout.Categories.Count - 1);
            categoryList.RefreshItems();
            if (selected >= 0) categoryList.SetSelection(selected);
            else BuildRightPane();
        }

#endregion

#region Right: settings of the selected category

        void BuildRightPane() {
            rightPane.Clear();

            if (selected < 0 || selected >= layout.Categories.Count) {
                rightPane.Add(new Label("Add or select a category on the left."));
                return;
            }

            var category = layout.Categories[selected];

            var nameField = new TextField("Category Name") { value = category.name };
            nameField.RegisterValueChangedCallback(e => {
                Undo.RecordObject(layout, "Rename Settings Category");
                category.name = e.newValue;
                EditorUtility.SetDirty(layout);
                categoryList.RefreshItems();
            });
            rightPane.Add(nameField);

            var modeField = new EnumField("Apply Mode", category.applyMode);
            modeField.RegisterValueChangedCallback(e => {
                Undo.RecordObject(layout, "Change Apply Mode");
                category.applyMode = (ApplyMode)e.newValue;
                EditorUtility.SetDirty(layout);
            });
            rightPane.Add(modeField);

            var resetToggle = new Toggle("Show Reset Button") { value = category.showResetButton };
            resetToggle.RegisterValueChangedCallback(e => {
                Undo.RecordObject(layout, "Change Reset Button");
                category.showResetButton = e.newValue;
                EditorUtility.SetDirty(layout);
            });
            rightPane.Add(resetToggle);

            var list = new ScrollView();
            list.style.flexGrow = 1;

            var searchField = new ToolbarSearchField { value = search };
            searchField.style.marginTop = 4;
            searchField.style.marginBottom = 4;
            searchField.style.width = Length.Auto();
            searchField.RegisterValueChangedCallback(e => { search = e.newValue ?? string.Empty; FillList(list, category); });
            rightPane.Add(searchField);
            rightPane.Add(list);

            FillList(list, category);
        }

        void FillList(ScrollView list, CategoryLayout category) {
            list.Clear();

            bool Matches(SettingDefinition d) =>
                search.Length == 0
                || d.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || d.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || d.Group.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

            var groups = catalog.Where(Matches)
                .GroupBy(d => d.Group)
                .OrderBy(g => g.Key == "Custom" ? 1 : 0).ThenBy(g => g.Key, StringComparer.Ordinal);

            foreach (var group in groups) {
                var foldout = new Foldout { text = group.Key, value = true };
                foreach (var def in group.OrderBy(d => d.DisplayName, StringComparer.Ordinal)) foldout.Add(MakeRow(category, def));
                list.Add(foldout);
            }

            // Entries pointing at settings that were deleted: let the user clean them up.
            var missing = category.entries.Where(e => !catalog.Any(d => d.Id == e.settingId) && (search.Length == 0 || (e.settingId ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            if (missing.Count > 0) {
                var foldout = new Foldout { text = MissingGroup, value = true };
                foreach (var entry in missing) foldout.Add(MakeMissingRow(category, entry));
                list.Add(foldout);
            }
        }

        VisualElement MakeRow(CategoryLayout category, SettingDefinition def) {
            var entry = category.entries.Find(e => e.settingId == def.Id);
            var displays = SettingDisplays.For(def.Type).ToList();

            var row = NewRow();
            var toggle = new Toggle { value = entry != null };
            var label = new Label(def.DisplayName) { tooltip = $"{def.Id} ({def.Type})" };
            label.style.flexGrow = 1;

            int index = entry != null ? displays.FindIndex(d => d.Id == entry.displayType) : 0;
            var dropdown = new DropdownField(displays.Select(d => d.DisplayName).ToList(), Mathf.Max(index, 0));
            dropdown.style.width = 180;
            dropdown.SetEnabled(entry != null);

            toggle.RegisterValueChangedCallback(e => {
                Undo.RecordObject(layout, "Toggle Setting");
                if (e.newValue) {
                    int i = Mathf.Max(displays.FindIndex(d => d.DisplayName == dropdown.value), 0);
                    entry = new EntryLayout { settingId = def.Id, displayType = displays[i].Id };
                    category.entries.Add(entry);
                }
                else {
                    category.entries.Remove(entry);
                    entry = null;
                }
                dropdown.SetEnabled(e.newValue);
                EditorUtility.SetDirty(layout);
            });

            dropdown.RegisterValueChangedCallback(e => {
                if (entry == null) return;
                int i = displays.FindIndex(d => d.DisplayName == e.newValue);
                if (i < 0) return;
                Undo.RecordObject(layout, "Change Setting Display");
                entry.displayType = displays[i].Id;
                EditorUtility.SetDirty(layout);
            });

            // A stored display type that no longer exists silently falls back to the default at runtime, show that here too.
            if (entry != null && index < 0 && displays.Count > 0) entry.displayType = displays[0].Id;

            row.Add(toggle);
            row.Add(label);
            row.Add(dropdown);
            return row;
        }

        VisualElement MakeMissingRow(CategoryLayout category, EntryLayout entry) {
            var row = NewRow();
            var toggle = new Toggle { value = true };
            var label = new Label(entry.settingId) { tooltip = "This setting no longer exists. Untick to remove it from the category." };
            label.style.flexGrow = 1;
            label.style.color = new Color(1f, 0.45f, 0.45f);

            toggle.RegisterValueChangedCallback(e => {
                if (e.newValue) return;
                Undo.RecordObject(layout, "Remove Missing Setting");
                category.entries.Remove(entry);
                EditorUtility.SetDirty(layout);
                row.RemoveFromHierarchy();
            });

            row.Add(toggle);
            row.Add(label);
            return row;
        }

        static VisualElement NewRow() {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 22;
            return row;
        }
    }

#endregion

}
