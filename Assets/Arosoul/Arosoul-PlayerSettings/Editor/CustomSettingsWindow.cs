using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings.EditorTools {

    /// <summary>
    /// Table editor for custom (value only) settings. Saving writes CustomSettings.cs, which gives other
    /// systems type safe access and is also where this window reads the settings back from.
    /// </summary>
    public sealed class CustomSettingsWindow : EditorWindow {
        [Serializable] sealed class RowList { public List<CustomSettingData> items = new List<CustomSettingData>(); }

        // Survive domain reloads (saving triggers a recompile), so unsaved edits are not lost.
        [SerializeField] string workingJson;
        [SerializeField] string outputFolder;

        List<CustomSettingData> rows = new List<CustomSettingData>();
        string savedJson = "";
        string existingPath;

        MultiColumnListView table;
        Button saveButton;
        HelpBox errorBox;

        [MenuItem("Tools/Arosoul/PlayerSettings/Custom Settings", priority = 104)]
        public static void Open() {
            var window = GetWindow<CustomSettingsWindow>();
            window.titleContent = new GUIContent("Custom Settings");
            window.minSize = new Vector2(820, 300);
        }

        public void CreateGUI() {
            SettingsRegistry.Rebuild();

            var loaded = CustomSettingsCodeGenerator.LoadExisting(out existingPath);
            savedJson = ToJson(loaded);
            rows = string.IsNullOrEmpty(workingJson) ? loaded : FromJson(workingJson);

            if (string.IsNullOrEmpty(outputFolder)) {
                outputFolder = existingPath != null
                    ? Path.GetDirectoryName(existingPath).Replace('\\', '/')
                    : CustomSettingsCodeGenerator.DefaultFolder;
            }

            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.style.paddingTop = root.style.paddingBottom = 8;

            root.Add(BuildToolbar());
            root.Add(BuildTable());

            errorBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            root.Add(errorBox);

            UpdateState();
        }

#region UI

        VisualElement BuildToolbar() {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.marginBottom = 6;

            var add = new Button(AddRow) { text = "Add Setting" };
            bar.Add(add);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            bar.Add(spacer);

            var folder = new TextField("Output Folder") { value = outputFolder };
            folder.style.minWidth = 320;
            folder.RegisterValueChangedCallback(e => { outputFolder = e.newValue; UpdateState(); });
            bar.Add(folder);

            saveButton = new Button(Save) { text = "Save" };
            saveButton.style.minWidth = 80;
            bar.Add(saveButton);
            return bar;
        }

        VisualElement BuildTable() {
            table = new MultiColumnListView {
                itemsSource = rows,
                fixedItemHeight = 24,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                sortingMode = ColumnSortingMode.None
            };
            table.style.flexGrow = 1;

            // The header of a MultiColumnListView stays in place while the rows scroll.
            AddTextColumn("api", "API Name", 170, r => r.apiName, (r, v) => r.apiName = v.Trim());
            AddTextColumn("display", "Display Name", 170, r => r.displayName, (r, v) => r.displayName = v);
            AddTypeColumn();
            AddNumberColumn("min", "Min", 70, r => r.min, (r, v) => r.min = v);
            AddNumberColumn("max", "Max", 70, r => r.max, (r, v) => r.max = v);
            AddTextColumn("default", "Default", 90, r => r.defaultValue, (r, v) => r.defaultValue = v, r => true);
            AddTextColumn("options", "Options (comma separated)", 200,
                r => string.Join(", ", r.options ?? new string[0]),
                (r, v) => r.options = v.Split(',').Select(o => o.Trim()).Where(o => o.Length > 0).ToArray(),
                r => r.type == SettingType.Enum);
            AddDeleteColumn();
            return table;
        }

        void AddTextColumn(string name, string title, float width, Func<CustomSettingData, string> get,
                           Action<CustomSettingData, string> set, Func<CustomSettingData, bool> enabled = null) {
            table.columns.Add(new Column {
                name = name, title = title, width = width, minWidth = 60, stretchable = true,
                makeCell = () => {
                    var field = new TextField();
                    field.RegisterValueChangedCallback(e => {
                        if (field.userData is int i && i < rows.Count) { set(rows[i], e.newValue); OnEdited(); }
                    });
                    return field;
                },
                bindCell = (element, i) => {
                    var field = (TextField)element;
                    field.userData = i;
                    field.SetValueWithoutNotify(get(rows[i]));
                    field.SetEnabled(enabled == null || enabled(rows[i]));
                }
            });
        }

        void AddNumberColumn(string name, string title, float width, Func<CustomSettingData, float> get, Action<CustomSettingData, float> set) {
            table.columns.Add(new Column {
                name = name, title = title, width = width, minWidth = 50, stretchable = true,
                makeCell = () => {
                    var field = new FloatField();
                    field.RegisterValueChangedCallback(e => {
                        if (field.userData is int i && i < rows.Count) {
                            float v = rows[i].type == SettingType.Int ? Mathf.Round(e.newValue) : e.newValue;
                            set(rows[i], v);
                            OnEdited();
                        }
                    });
                    return field;
                },
                bindCell = (element, i) => {
                    var field = (FloatField)element;
                    field.userData = i;
                    field.SetValueWithoutNotify(get(rows[i]));
                    field.SetEnabled(rows[i].type == SettingType.Int || rows[i].type == SettingType.Float);
                }
            });
        }

        void AddTypeColumn() {
            table.columns.Add(new Column {
                name = "type", title = "Type", width = 100, minWidth = 80, stretchable = true,
                makeCell = () => {
                    var field = new EnumField(SettingType.String);
                    field.RegisterValueChangedCallback(e => {
                        if (field.userData is int i && i < rows.Count) {
                            rows[i].type = (SettingType)e.newValue;
                            ApplyTypeDefaults(rows[i]);
                            OnEdited();
                            table.schedule.Execute(() => table.RefreshItems()); // enabled states of other cells changed
                        }
                    });
                    return field;
                },
                bindCell = (element, i) => {
                    var field = (EnumField)element;
                    field.userData = i;
                    field.SetValueWithoutNotify(rows[i].type);
                }
            });
        }

        void AddDeleteColumn() {
            table.columns.Add(new Column {
                name = "delete", title = "", width = 40, minWidth = 40, maxWidth = 40, stretchable = false, resizable = false,
                makeCell = () => {
                    var button = new Button { text = "X" };
                    button.clicked += () => {
                        if (button.userData is int i && i < rows.Count) {
                            rows.RemoveAt(i);
                            table.RefreshItems();
                            OnEdited();
                        }
                    };
                    return button;
                },
                bindCell = (element, i) => element.userData = i
            });
        }

#endregion

#region Editing

        void AddRow() {
            string name = "NewSetting";
            for (int n = 2; rows.Any(r => string.Equals(r.apiName, name, StringComparison.OrdinalIgnoreCase)); n++) name = "NewSetting" + n;

            rows.Add(new CustomSettingData { apiName = name, displayName = SettingValues.Nicify(name) });
            table.RefreshItems();
            table.ScrollToItem(-1);
            OnEdited();
        }

        static void ApplyTypeDefaults(CustomSettingData r) {
            switch (r.type) {
                case SettingType.Float: r.min = 0f; r.max = 1f; r.defaultValue = "0"; break;
                case SettingType.Int: r.min = 0f; r.max = 100f; r.defaultValue = "0"; break;
                case SettingType.Bool: r.defaultValue = "false"; break;
                case SettingType.Enum:
                    if (r.options == null || r.options.Length == 0) r.options = new[] { "Off", "On" };
                    r.defaultValue = r.options[0];
                    break;
                default: r.defaultValue = string.Empty; break;
            }
        }

        void OnEdited() {
            workingJson = ToJson(rows);
            UpdateState();
        }

        void UpdateState() {
            var errors = CustomSettingsCodeGenerator.Validate(rows);
            bool dirty = ToJson(rows) != savedJson || (existingPath != null && ExistingFolder() != outputFolder.TrimEnd('/'));
            bool folderOk = outputFolder != null && (outputFolder.TrimEnd('/') == "Assets" || outputFolder.StartsWith("Assets/"));
            if (!folderOk) errors.Add("Output folder must be inside the Assets folder.");

            saveButton.SetEnabled(dirty && errors.Count == 0);
            hasUnsavedChanges = dirty;
            errorBox.style.display = errors.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            errorBox.text = string.Join("\n", errors);
        }

        string ExistingFolder() => Path.GetDirectoryName(existingPath).Replace('\\', '/');

#endregion

#region Saving

        public override void SaveChanges() {
            Save();
            base.SaveChanges();
        }

        void Save() {
            string folder = outputFolder.TrimEnd('/');
            string path = folder + "/" + CustomSettingsCodeGenerator.ClassName + ".cs";

            Directory.CreateDirectory(folder);
            if (existingPath != null && existingPath != path) AssetDatabase.DeleteAsset(existingPath); // moved, avoid a duplicate class

            CustomSettingsCodeGenerator.Write(path, rows);

            existingPath = path;
            savedJson = ToJson(rows);
            UpdateState();
        }

        static string ToJson(List<CustomSettingData> list) => JsonUtility.ToJson(new RowList { items = list });
        static List<CustomSettingData> FromJson(string json) => JsonUtility.FromJson<RowList>(json).items ?? new List<CustomSettingData>();
    }

#endregion

}
