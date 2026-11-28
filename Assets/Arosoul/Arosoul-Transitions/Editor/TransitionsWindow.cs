using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Overview of all transitions. Create new ones, inspect and preview them, and see their generated ids.
    /// </summary>
    public class TransitionsWindow : EditorWindow
    {
        private List<TransitionDefinition> _definitions = new List<TransitionDefinition>();
        private ListView _list;
        private VisualElement _detail;
        private HelpBox _issues;
        private ToolbarButton _deleteButton;

        [MenuItem("Tools/Arosoul/Transitions/Transitions", priority = 110)]
        public static void Open()
        {
            var window = GetWindow<TransitionsWindow>();
            window.titleContent = new GUIContent("Transitions");
            window.minSize = new Vector2(560, 320);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            var toolbar = new Toolbar();
            var create = new ToolbarMenu { text = "New Transition" };
            foreach (Type type in TypeCache.GetTypesDerivedFrom<TransitionDefinition>().Where(t => !t.IsAbstract).OrderBy(t => t.Name))
            {
                Type captured = type;
                create.menu.AppendAction(ObjectNames.NicifyVariableName(type.Name), _ => Create(captured));
            }
            toolbar.Add(create);
            toolbar.Add(new ToolbarButton(() => { TransitionRegistryBuilder.Refresh(); Reload(); }) { text = "Regenerate Ids" });

            _deleteButton = new ToolbarButton(DeleteSelected) { text = "Delete Selected" };
            _deleteButton.SetEnabled(false);
            toolbar.Add(_deleteButton);
            root.Add(toolbar);

            var split = new TwoPaneSplitView(0, 280, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;

            _list = new ListView
            {
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                makeItem = MakeListItem,
                bindItem = BindListItem
            };
            _list.selectionChanged += _ => ShowSelected();
            split.Add(_list);

            _detail = new ScrollView();
            _detail.style.paddingLeft = 8;
            _detail.style.paddingRight = 8;
            split.Add(_detail);
            root.Add(split);

            _issues = new HelpBox("", HelpBoxMessageType.Warning);
            _issues.style.display = DisplayStyle.None;
            root.Add(_issues);

            Reload();
        }

        private void OnProjectChange()
        {
            if (_list != null) Reload();
        }

        private void Reload()
        {
            _definitions = TransitionRegistryBuilder.FindDefinitions();
            _list.itemsSource = _definitions;
            _list.Rebuild();

            var duplicates = _definitions.GroupBy(d => d.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            _issues.text = "Duplicate transition keys: " + string.Join(", ", duplicates) + ". Generated constants get a numeric suffix.";
            _issues.style.display = duplicates.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement MakeListItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 1;
            row.style.paddingLeft = 6;

            var key = new Label { name = "key" };
            row.Add(key);

            // Darker than the key, so it is clear that it is not part of the name.
            var type = new Label { name = "type" };
            type.style.marginLeft = 8;
            type.style.color = new Color(0.5f, 0.5f, 0.5f);
            type.style.fontSize = 10;
            row.Add(type);
            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            var definition = _definitions[index];
            element.Q<Label>("key").text = definition == null ? "(missing)" : definition.Key;
            element.Q<Label>("type").text = definition == null ? "" : definition.GetType().Name;
        }

        private TransitionDefinition GetSelected()
        {
            return _list.selectedItem as TransitionDefinition;
        }

        private void DeleteSelected()
        {
            TransitionDefinition definition = GetSelected();
            if (definition == null) return;

            string path = AssetDatabase.GetAssetPath(definition);
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Transition",
                $"Delete the transition '{definition.Key}'?\n\n{path}\n\nThis deletes the asset and can not be undone. Anything that references it will lose the reference.",
                "Delete",
                "Cancel");
            if (!confirmed) return;

            _list.ClearSelection();
            AssetDatabase.DeleteAsset(path);
            TransitionRegistryBuilder.Refresh();
            Reload();
        }

        private void ShowSelected()
        {
            _detail.Clear();
            TransitionDefinition definition = GetSelected();
            _deleteButton.SetEnabled(definition != null);
            if (definition != null)
                _detail.Add(new InspectorElement(definition));
        }

        private void Create(Type type)
        {
            var definition = TransitionRegistryBuilder.CreateDefinition(type, type.Name.Replace("Transition", ""));
            TransitionRegistryBuilder.Refresh();
            Reload();
            _list.SetSelection(_definitions.IndexOf(definition));
        }
    }
}
