using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Editor for Scenes settings: core scene, registered scenes and their dependencies, and slots.
    /// Changes are saved to ProjectSettings and baked into the runtime catalog automatically.
    /// </summary>
    public class SceneConfigWindow : EditorWindow
    {
        // Serialized on the window, so they survive recompiles and domain reloads.
        [SerializeField] private int _selectedTab;
        [SerializeField] private string _search = "";

        private SerializedObject _serializedObject;
        private IVisualElementScheduledItem _commitItem;
        private VisualElement _issues;
        private Label _status;
        private Button _idsButton;

        private string _snapshot;

        [MenuItem("Tools/Scene System/Scene Config")]
        public static void Open()
        {
            var window = GetWindow<SceneConfigWindow>();
            window.titleContent = new GUIContent("Scene Config");
            window.minSize = new Vector2(480, 400);
        }

        private void OnEnable()
        {
            ScenesSettingsUtility.Changed += OnExternalChange;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            ScenesSettingsUtility.Changed -= OnExternalChange;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        // Rebuild after the current event has finished, the change can come from a button inside this window.
        private void OnExternalChange()
        {
            EditorApplication.delayCall += () =>
            {
                if (this != null) RebuildGui();
            };
        }

        private void OnUndoRedo()
        {
            if (_serializedObject == null) return;

            // Undo and redo also fire for unrelated changes, ignore those.
            if (Snapshot() == _snapshot) return;

            RebuildGui();
            ScheduleCommit(); // save and bake the reverted state
        }

        private void RebuildGui()
        {
            rootVisualElement.Clear();
            CreateGUI();
        }

        private void CreateGUI()
        {
            var settings = ScenesSettings.instance;
            settings.EnsureDefaults();
            _serializedObject = new SerializedObject(settings);

            var root = new VisualElement();
            root.style.flexGrow = 1;
            rootVisualElement.Add(root);

            var tabs = new TabView();
            tabs.style.flexGrow = 1;
            tabs.Add(MakeTab("General", BuildGeneral()));

            Tab scenesTab = MakeTab("Scenes", BuildScenes());
            tabs.Add(scenesTab);
            RegisterSceneDragAndDrop(scenesTab);

            tabs.Add(MakeTab("Slots", BuildSlots()));

            _issues = new VisualElement();
            tabs.Add(MakeTab("Validation", _issues));
            root.Add(tabs);

            // Restore the selected tab, and remember it when it changes.
            tabs.selectedTabIndex = Mathf.Clamp(_selectedTab, 0, 3);
            tabs.activeTabChanged += (_, __) => _selectedTab = tabs.selectedTabIndex;

            var statusBar = new VisualElement();
            statusBar.style.flexDirection = FlexDirection.Row;
            statusBar.style.alignItems = Align.Center;
            statusBar.style.paddingLeft = 8;
            statusBar.style.paddingRight = 4;
            statusBar.style.paddingTop = 2;
            statusBar.style.paddingBottom = 2;
            statusBar.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);

            _status = new Label();
            _status.style.flexGrow = 1;
            statusBar.Add(_status);

            _idsButton = new Button(RebuildIds) { text = "Rebuild SceneIds" };
            _idsButton.style.flexShrink = 0;
            _idsButton.style.marginTop = 0;
            _idsButton.style.marginBottom = 0;
            statusBar.Add(_idsButton);

            root.Add(statusBar);

            root.Bind(_serializedObject);
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
            {
                if (Undo.GetCurrentGroupName().StartsWith("Modified", StringComparison.Ordinal))
                    Undo.SetCurrentGroupName($"Change {evt.changedProperty.displayName}");
            });
            root.TrackSerializedObjectValue(_serializedObject, _ => ScheduleCommit());

            RefreshValidation(ScenesValidator.Validate(settings.Data.Clone(), new EditorSceneProjectContext(), settings.SyncBuildSettings));
            _snapshot = Snapshot();
        }

        private static Tab MakeTab(string title, VisualElement content)
        {
            var tab = new Tab(title);
            tab.style.flexGrow = 1;
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            scroll.Add(content);
            tab.Add(scroll);
            return tab;
        }

        // Saving and baking writes assets, so wait until the user has stopped typing.
        private void ScheduleCommit()
        {
            _commitItem ??= rootVisualElement.schedule.Execute(Commit);
            _commitItem.ExecuteLater(500);
        }

        private void Commit()
        {
            // The bindings already apply and record edits. Committing must never record anything itself,
            // or it would clear the redo stack after an undo.
            _serializedObject.ApplyModifiedPropertiesWithoutUndo();
            _snapshot = Snapshot();
            RefreshValidation(ScenesSettingsUtility.Commit());
        }

        private void RefreshValidation(ValidationReport report)
        {
            _issues.Clear();

            var run = new Button(() => RefreshValidation(ScenesSettingsUtility.Commit())) { text = "Validate and Rebuild" };
            run.style.marginLeft = run.style.marginTop = run.style.marginRight = run.style.marginBottom = 8;
            _issues.Add(run);

            if (report.Issues.Count == 0)
                _issues.Add(new HelpBox("No problems found.", HelpBoxMessageType.Info));
            foreach (var issue in report.Issues)
            {
                var box = EditorUI.Issue(issue);
                box.style.marginLeft = 8;
                box.style.marginRight = 8;
                _issues.Add(box);
            }

            _status.text = report.ErrorCount == 0 && report.WarningCount == 0
                ? "No problems"
                : $"{report.ErrorCount} error(s), {report.WarningCount} warning(s)";
            _status.style.color = report.HasErrors ? new Color(1f, 0.45f, 0.4f) : report.WarningCount > 0 ? new Color(1f, 0.8f, 0.4f) : new Color(0.6f, 0.9f, 0.6f);

            RefreshIdsButton();
        }

        #region Tabs

        private VisualElement BuildGeneral()
        {
            var section = EditorUI.Section("Core Scene");
            section.Add(new HelpBox("The core scene is a dependency of every scene and is never unloaded. Use it for managers and services that must always exist.", HelpBoxMessageType.None));

            section.Add(new PropertyField(_serializedObject.FindProperty("_data._loadCoreScene"), "Load Core Scene"));
            section.Add(new PropertyField(_serializedObject.FindProperty("_data._coreScene"), "Core Scene"));

            section.Add(EditorUI.Header("Startup"));
            section.Add(new PropertyField(_serializedObject.FindProperty("_data._initialSlot"), "Initial Slot"));

            section.Add(EditorUI.Header("Editor"));
            section.Add(new PropertyField(_serializedObject.FindProperty("_playModeBootstrap"), "Play Mode Bootstrap"));
            section.Add(new PropertyField(_serializedObject.FindProperty("_syncBuildSettings"), "Sync Build Settings"));
            return section;
        }

        private VisualElement BuildScenes()
        {
            var section = EditorUI.Section("Registered Scenes");
            section.Add(new HelpBox("Dependencies of a scene are loaded before it. Pressing play in a scene loads its dependencies first. Drag scenes from the Project window onto this tab to register them.", HelpBoxMessageType.None));

            var search = new ToolbarSearchField { value = _search };
            search.style.marginLeft = 0;
            search.style.marginRight = 0;
            search.style.marginBottom = 4;
            search.style.width = Length.Percent(100);
            section.Add(search);

            VisualElement list = BuildArrayEditor("_data._scenes", "Register Scene", "Scene", element =>
            {
                var card = EditorUI.Card();
                card.Add(new PropertyField(element.FindPropertyRelative("_scene"), "Scene"));
                card.Add(new PropertyField(element.FindPropertyRelative("_dependencies"), "Dependencies"));
                return card;
            }, element =>
            {
                element.FindPropertyRelative("_scene._guid").stringValue = "";
                element.FindPropertyRelative("_scene._path").stringValue = "";
                element.FindPropertyRelative("_scene._name").stringValue = "";
                element.FindPropertyRelative("_dependencies").ClearArray();
            }, SceneNameOf);
            section.Add(list);

            var applyFilter = (Action<string>)list.userData;
            applyFilter(_search);
            search.RegisterValueChangedCallback(evt =>
            {
                _search = evt.newValue ?? "";
                applyFilter(_search);
            });

            var folder = new Button(RegisterFolder) { text = "Register All Scenes In Folder" };
            folder.style.marginTop = 2;
            section.Add(folder);
            return section;
        }

        private static string SceneNameOf(SerializedProperty element)
        {
            string name = element.FindPropertyRelative("_scene._name").stringValue;
            if (!string.IsNullOrEmpty(name)) return name;

            string path = element.FindPropertyRelative("_scene._path").stringValue;
            return string.IsNullOrEmpty(path) ? "" : System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private VisualElement BuildSlots()
        {
            var section = EditorUI.Section("Slots");
            section.Add(new HelpBox("A slot holds at most one scene, loading a scene into a slot replaces what was there. The key becomes a generated constant: SlotIds.<Key>.", HelpBoxMessageType.None));

            section.Add(BuildArrayEditor("_data._slots", "Add Slot", "Slot", element =>
            {
                var row = EditorUI.Row();
                var key = new PropertyField(element.FindPropertyRelative("_key"), "");
                key.style.flexGrow = 1;
                row.Add(key);
                row.Add(EditorUI.Muted($"#{element.FindPropertyRelative("_id").intValue}"));
                return row;
            }, element =>
            {
                SerializedProperty next = _serializedObject.FindProperty("_data._nextSlotId");
                element.FindPropertyRelative("_id").intValue = next.intValue++;
                element.FindPropertyRelative("_key").stringValue = "NewSlot";
            }));
            return section;
        }

        #endregion

        #region Registering scenes

        private void RegisterFolder()
        {
            _serializedObject.ApplyModifiedProperties();

            string selected = EditorUtility.OpenFolderPanel("Select a folder under Assets", "Assets", "");
            if (string.IsNullOrEmpty(selected)) return;

            string relative = FileUtil.GetProjectRelativePath(selected);
            if (string.IsNullOrEmpty(relative) || !(relative == "Assets" || relative.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                EditorUtility.DisplayDialog("Invalid folder", "The folder must be inside the Assets folder of this project.", "OK");
                return;
            }

            int added = ScenesSettingsUtility.RegisterScenesInFolder(relative);
            ShowNotification(new GUIContent(added == 0 ? "No new scenes found" : $"Registered {added} scene(s)"));
        }

        private void RegisterSceneDragAndDrop(VisualElement target)
        {
            Color highlight = new Color(0.3f, 0.55f, 0.9f, 0.18f);

            static bool HasScenes() => DragAndDrop.objectReferences.Any(o => o is SceneAsset);

            target.RegisterCallback<DragEnterEvent>(evt =>
            {
                if (HasScenes()) target.style.backgroundColor = highlight;
            });
            target.RegisterCallback<DragLeaveEvent>(_ => target.style.backgroundColor = StyleKeyword.Null);
            target.RegisterCallback<DragExitedEvent>(_ => target.style.backgroundColor = StyleKeyword.Null);

            target.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (!HasScenes()) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            });

            target.RegisterCallback<DragPerformEvent>(evt =>
            {
                target.style.backgroundColor = StyleKeyword.Null;
                if (!HasScenes()) return;

                DragAndDrop.AcceptDrag();
                _serializedObject.ApplyModifiedProperties();

                var guids = new List<string>();
                foreach (UnityEngine.Object dropped in DragAndDrop.objectReferences)
                {
                    if (dropped is SceneAsset)
                        guids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(dropped)));
                }

                int added = ScenesSettingsUtility.RegisterScenes(guids);
                ShowNotification(new GUIContent(added == 0 ? "Scenes are already registered" : $"Registered {added} scene(s)"));
                evt.StopPropagation();
            });
        }

        #endregion

        private void RebuildIds()
        {
            _serializedObject.ApplyModifiedProperties(); // include edits that are still waiting for the debounce
            SceneCatalogBaker.GenerateIds();
            RefreshIdsButton();
        }

        private void RefreshIdsButton()
        {
            bool outOfSync = SceneCatalogBaker.IsIdsOutOfSync();

            _idsButton.SetEnabled(outOfSync && !EditorApplication.isPlayingOrWillChangePlaymode);
            _idsButton.tooltip = outOfSync
                ? "The generated SceneIds script does not match the current configuration. Click to rebuild it."
                : "The generated SceneIds script is up to date.";

            if (outOfSync)
                _idsButton.style.backgroundColor = new Color(0.85f, 0.55f, 0.15f, 0.9f);
            else
                _idsButton.style.backgroundColor = StyleKeyword.Null;
            _idsButton.style.unityFontStyleAndWeight = outOfSync ? FontStyle.Bold : FontStyle.Normal;
        }

        // Scenes can be renamed or moved in the Project window.
        private void OnProjectChange()
        {
            if (_idsButton != null) RefreshIdsButton();
        }

        /// <summary>
        /// A list editor with add and remove buttons. Rows are rebuilt when the number of elements changes.
        /// If a search key is given, the returned element's userData is an Action of string that filters the rows by it.
        /// </summary>
        private VisualElement BuildArrayEditor(string arrayPath, string addLabel, string itemName, Func<SerializedProperty, VisualElement> makeRow, Action<SerializedProperty> initializeNew, Func<SerializedProperty, string> searchKey = null)
        {
            var container = new VisualElement();
            var rows = new VisualElement();
            container.Add(rows);

            var entries = new List<(VisualElement line, string key)>();
            string filter = "";

            void ApplyFilter()
            {
                foreach (var (line, key) in entries)
                {
                    // Entries without a name yet (just added) stay visible so they can be filled in.
                    bool visible = string.IsNullOrWhiteSpace(filter) || string.IsNullOrEmpty(key) || key.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
                    line.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            void Rebuild()
            {
                rows.Clear();
                entries.Clear();

                SerializedProperty array = _serializedObject.FindProperty(arrayPath);
                for (int i = 0; i < array.arraySize; i++)
                {
                    int index = i;
                    SerializedProperty element = array.GetArrayElementAtIndex(i);

                    var line = EditorUI.Row();
                    line.style.alignItems = Align.FlexStart;

                    VisualElement content = makeRow(element);
                    content.style.flexGrow = 1;
                    line.Add(content);

                    var remove = new Button(() =>
                    {
                        _serializedObject.Update();
                        _serializedObject.FindProperty(arrayPath).DeleteArrayElementAtIndex(index);
                        ScenesUndo.Apply(_serializedObject, $"Remove {itemName}");
                        Rebuild();
                    }) { text = "✕", tooltip = "Remove" };
                    remove.style.width = 22;
                    line.Add(remove);

                    rows.Add(line);
                    entries.Add((line, searchKey?.Invoke(element)));
                }

                rows.Bind(_serializedObject);
                ApplyFilter();
            }

            var add = new Button(() =>
            {
                _serializedObject.Update();
                SerializedProperty array = _serializedObject.FindProperty(arrayPath);
                array.arraySize++;
                initializeNew?.Invoke(array.GetArrayElementAtIndex(array.arraySize - 1));
                ScenesUndo.Apply(_serializedObject, addLabel);
                Rebuild();
            }) { text = addLabel };
            add.style.marginTop = 2;
            container.Add(add);

            container.userData = (Action<string>)(text =>
            {
                filter = text ?? "";
                ApplyFilter();
            });

            Rebuild();
            return container;
        }

        private static string Snapshot()
        {
            return EditorJsonUtility.ToJson(ScenesSettings.instance);
        }
    }
}
