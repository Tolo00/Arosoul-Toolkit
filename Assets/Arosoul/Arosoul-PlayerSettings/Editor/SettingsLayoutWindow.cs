using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.IMGUI.Controls;
using System.Globalization;

namespace Arosoul.PlayerSettings.EditorTools {

    /// <summary>
    /// Edits the categories of a SettingsLayout. Left: the categories (tabs). Right: every available setting,
    /// grouped and searchable, with a toggle (include in this category) and a dropdown (how it is displayed).
    /// </summary>
    public sealed class SettingsLayoutWindow : EditorWindow {
        [SerializeField] SettingsLayout layout;

        sealed class CatalogItem {
            public string Id, Name, Group, Tooltip;
            public List<KeyValuePair<string, string>> Displays = new List<KeyValuePair<string, string>>(); // id, name
            public LayoutDecoration Decoration;
        }

        List<CatalogItem> catalog = new List<CatalogItem>();

        int selected = -1;
        ListView entryList;
        readonly AdvancedDropdownState dropdownState = new AdvancedDropdownState();
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

            catalog = BuildCatalog();

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

        static List<CatalogItem> BuildCatalog() {
            SettingsRegistry.Rebuild();
            var items = new List<CatalogItem>();

            foreach (var def in SettingsRegistry.All) {
                var item = new CatalogItem { Id = def.Id, Name = def.DisplayName, Group = def.Group, Tooltip = $"{def.Id} ({def.Type})" };
                foreach (var display in SettingDisplays.For(def.Type)) item.Displays.Add(new KeyValuePair<string, string>(display.Id, display.DisplayName));
                items.Add(item);
            }

            foreach (var section in SettingsRegistry.Sections) {
                var item = new CatalogItem { Id = section.Id, Name = section.DisplayName, Group = section.Group, Tooltip = section.Id };
                foreach (var display in section.Displays) item.Displays.Add(new KeyValuePair<string, string>(display.Id, display.Name));
                items.Add(item);
            }

            foreach (var decoration in SettingsRegistry.Decorations) {
                items.Add(new CatalogItem { Id = decoration.Id, Name = decoration.DisplayName, Group = decoration.Group, Tooltip = decoration.Id, Decoration = decoration });
            }
            return items;
        }

        void OnFocus() {
            if (layout == null || rightPane == null) return;
            catalog = BuildCatalog();
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

            Button addButton = null;
            addButton = new Button(() => ShowAddMenu(category, addButton.worldBound)) { text = "Add" };
            addButton.style.marginTop = 4;
            addButton.style.marginBottom = 4;
            rightPane.Add(addButton);

            entryList = new ListView {
                itemsSource = category.entries,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = NewRow,
                bindItem = (e, i) => BindEntry(e, category, category.entries[i])
            };
            entryList.style.flexGrow = 1;
            entryList.itemIndexChanged += (from, to) => EditorUtility.SetDirty(layout); // no Undo for reorder, same as categories
            rightPane.Add(entryList);
        }

        void BindEntry(VisualElement row, CategoryLayout category, EntryLayout entry) {
            row.Clear();
            row.style.paddingLeft = 4;
            row.style.paddingRight = 4;

            var item = catalog.Find(c => c.Id == entry.settingId);

            var label = new Label(item != null ? item.Name : entry.settingId) {
                tooltip = item != null ? item.Tooltip : "This setting no longer exists. Remove it from the category."
            };
            label.style.flexGrow = 1;
            if (item == null) label.style.color = new Color(1f, 0.45f, 0.45f);
            row.Add(label);

            if (item != null) AddDisplayDropdown(row, item, entry);
            if (item?.Decoration != null) AddParameterFields(row, item.Decoration, entry);

            var remove = new Button(() => RemoveEntry(category, entry)) { tooltip = "Remove" };
            remove.Add(new Image { image = EditorGUIUtility.IconContent("Toolbar Minus").image });
            row.Add(remove);
        }

        void AddDisplayDropdown(VisualElement row, CatalogItem item, EntryLayout entry) {
            var displays = item.Displays;
            if (displays.Count == 0) return;

            int index = displays.FindIndex(d => d.Key == entry.displayType);
            if (index < 0) {
                index = 0;
                entry.displayType = displays[0].Key;
                EditorUtility.SetDirty(layout);
            }

            var dropdown = new DropdownField(displays.Select(d => d.Value).ToList(), index);
            dropdown.style.width = 180;
            dropdown.RegisterValueChangedCallback(e => {
                int i = displays.FindIndex(d => d.Value == e.newValue);
                if (i < 0) return;
                Undo.RecordObject(layout, "Change Setting Display");
                entry.displayType = displays[i].Key;
                EditorUtility.SetDirty(layout);
            });
            row.Add(dropdown);
        }

        void AddParameterFields(VisualElement row, LayoutDecoration decoration, EntryLayout entry) {
            foreach (var parameter in decoration.Parameters) {
                var p = parameter;
                string current = entry.GetParameter(p.Key) ?? p.Default;

                if (p.Kind == DecorationParameterKind.Number) {
                    float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out float number);
                    var field = new FloatField(p.Label) { value = number };
                    field.RegisterValueChangedCallback(e => SetParameter(entry, p.Key, e.newValue.ToString(CultureInfo.InvariantCulture)));
                    field.labelElement.style.minWidth = 40;
                    field.style.width = 200;
                    row.Add(field);
                }
                else {
                    var field = new TextField(p.Label) { value = current };
                    field.RegisterValueChangedCallback(e => SetParameter(entry, p.Key, e.newValue));
                    field.labelElement.style.minWidth = 40;
                    field.style.width = 200;
                    row.Add(field);
                }
            }
        }

        void SetParameter(EntryLayout entry, string key, string value) {
            Undo.RecordObject(layout, "Change Decoration");
            entry.SetParameter(key, value);
            EditorUtility.SetDirty(layout);
        }

        void RemoveEntry(CategoryLayout category, EntryLayout entry) {
            Undo.RecordObject(layout, "Remove Entry");
            category.entries.Remove(entry); // reference equality, so duplicates are safe
            EditorUtility.SetDirty(layout);
            entryList.RefreshItems();
        }

        void ShowAddMenu(CategoryLayout category, Rect buttonRect) {
            var dropdown = new AddDropdown(dropdownState, catalog, id => {
                var item = catalog.Find(c => c.Id == id);
                if (item == null) return;

                Undo.RecordObject(layout, "Add Entry");
                category.entries.Add(new EntryLayout {
                    settingId = item.Id,
                    displayType = item.Displays.Count > 0 ? item.Displays[0].Key : null
                });
                EditorUtility.SetDirty(layout);
                entryList.RefreshItems();
                entryList.schedule.Execute(() => entryList.ScrollToItem(-1));
            });
            dropdown.Show(buttonRect);
        }

        static int GroupOrder(string group) => group == "Custom" ? 1 : group == "Decorations" ? 2 : 0;

        sealed class AddDropdown : AdvancedDropdown {
            sealed class Item : AdvancedDropdownItem {
                public readonly string Id;
                public Item(string id, string name) : base(name) { Id = id; }
            }

            readonly List<CatalogItem> items;
            readonly Action<string> onSelected;

            public AddDropdown(AdvancedDropdownState state, List<CatalogItem> items, Action<string> onSelected) : base(state) {
                this.items = items;
                this.onSelected = onSelected;
                minimumSize = new Vector2(260, 320);
            }

            protected override AdvancedDropdownItem BuildRoot() {
                var root = new AdvancedDropdownItem("Add");
                var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);

                AdvancedDropdownItem GetGroup(string path) {
                    if (groups.TryGetValue(path, out var found)) return found;

                    int slash = path.LastIndexOf('/');
                    var parent = slash < 0 ? root : GetGroup(path.Substring(0, slash));
                    var created = new AdvancedDropdownItem(slash < 0 ? path : path.Substring(slash + 1));
                    parent.AddChild(created);
                    groups.Add(path, created);
                    return created;
                }

                foreach (var item in items.OrderBy(i => GroupOrder(i.Group)).ThenBy(i => i.Group, StringComparer.Ordinal).ThenBy(i => i.Name, StringComparer.Ordinal)) {
                    var parent = string.IsNullOrEmpty(item.Group) ? root : GetGroup(item.Group);
                    parent.AddChild(new Item(item.Id, item.Name));
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item) {
                if (item is Item leaf) onSelected(leaf.Id);
            }
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
