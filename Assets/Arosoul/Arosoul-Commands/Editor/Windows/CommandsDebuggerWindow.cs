using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Lists every command in the project, grouped by the class that declares it, with setup problems.
    /// Double-click a command to open its script.
    /// </summary>
    public sealed class CommandsDebuggerWindow : EditorWindow
    {
        private static readonly Color DimTextColor = new(0.6f, 0.6f, 0.6f, 1f);

        private ToolbarSearchField _search;
        private Label _summary;
        private VisualElement _issues;
        private TreeView _tree;
        private Label _details;

        [MenuItem("Tools/Arosoul/Commands/Debugger", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<CommandsDebuggerWindow>();
            window.titleContent = new GUIContent("Commands Debugger");
            window.minSize = new Vector2(420, 300);
        }

        private void CreateGUI()
        {
            var toolbar = new Toolbar();
            _search = new ToolbarSearchField();
            _search.style.flexGrow = 1;
            _search.RegisterValueChangedCallback(_ => Rebuild());
            toolbar.Add(_search);
            toolbar.Add(new ToolbarButton(CommandsSettingsWindow.Open) { text = "Settings" });
            rootVisualElement.Add(toolbar);

            _summary = new Label();
            _summary.style.marginLeft = 6;
            _summary.style.marginTop = 4;
            _summary.style.marginBottom = 4;
            rootVisualElement.Add(_summary);

            _issues = new VisualElement();
            rootVisualElement.Add(_issues);

            var split = new TwoPaneSplitView(1, 110, TwoPaneSplitViewOrientation.Vertical);
            split.style.flexGrow = 1;
            rootVisualElement.Add(split);

            _tree = new TreeView
            {
                makeItem = MakeRow,
                bindItem = BindRow,
                selectionType = SelectionType.Single,
                fixedItemHeight = 20
            };
            _tree.selectionChanged += OnSelectionChanged;
            _tree.itemsChosen += OnItemsChosen;
            split.Add(_tree);

            var detailsScroll = new ScrollView();
            _details = new Label { enableRichText = false };
            _details.style.whiteSpace = WhiteSpace.Normal;
            _details.style.marginLeft = 6;
            _details.style.marginTop = 4;
            _details.selection.isSelectable = true;
            detailsScroll.Add(_details);
            split.Add(detailsScroll);

            Rebuild();
        }

        private void Rebuild()
        {
            CommandRegistry registry = CommandRegistry.Default;
            string filter = _search?.value?.Trim() ?? string.Empty;

            int id = 0;
            var roots = new List<TreeViewItemData<object>>();
            List<IGrouping<Type, CommandInfo>> groups = registry.Commands
                .Where(c => Matches(c, filter))
                .GroupBy(c => c.DeclaringType)
                .OrderBy(g => g.Key.FullName)
                .ToList();

            foreach (IGrouping<Type, CommandInfo> group in groups)
            {
                List<TreeViewItemData<object>> children = group.Select(c => new TreeViewItemData<object>(id++, c)).ToList();
                roots.Add(new TreeViewItemData<object>(id++, group.Key, children));
            }

            _tree.SetRootItems(roots);
            _tree.Rebuild();
            _tree.ExpandAll();

            int shown = groups.Sum(g => g.Count());
            int errors = registry.Issues.Count(i => i.Severity == CommandIssueSeverity.Error);
            int warnings = registry.Issues.Count - errors;
            string filtered = filter.Length > 0 ? $"{shown} of " : string.Empty;
            _summary.text = $"{filtered}{registry.Commands.Count} commands in {groups.Count} classes. {errors} errors, {warnings} warnings.";

            _issues.Clear();
            foreach (CommandIssue issue in registry.Issues)
            {
                var box = new HelpBox(issue.Message, issue.Severity == CommandIssueSeverity.Error ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning);
                if (issue.Member != null)
                {
                    box.tooltip = "Click to open the script.";
                    box.RegisterCallback<ClickEvent>(_ => CommandSourceLocator.TryOpen(issue.Member));
                }
                _issues.Add(box);
            }

            _details.text = "Select a command to see its details. Double-click to open its script.";
        }

        private static bool Matches(CommandInfo command, string filter)
        {
            if (filter.Length == 0) return true;

            return Contains(command.Name, filter) ||
                   Contains(command.Description, filter) ||
                   Contains(command.DeclaringType.FullName, filter) ||
                   Contains(command.Member.Name, filter);
        }

        private static bool Contains(string text, string filter)
        {
            return text != null && text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        #region Rows

        private static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = new Label { name = "name" };
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(name);

            var signature = new Label { name = "signature" };
            signature.style.marginLeft = 6;
            row.Add(signature);

            var info = new Label { name = "info" };
            info.style.marginLeft = 8;
            info.style.color = DimTextColor;
            info.style.flexShrink = 1;
            info.style.overflow = Overflow.Hidden;
            info.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(info);
            return row;
        }

        private void BindRow(VisualElement row, int index)
        {
            object item = _tree.GetItemDataForIndex<object>(index);
            var name = row.Q<Label>("name");
            var signature = row.Q<Label>("signature");
            var info = row.Q<Label>("info");

            if (item is Type type)
            {
                name.text = TypeNames.GetFriendlyName(type);
                signature.text = string.Empty;
                info.text = $"{type.Namespace ?? "global"} · {type.Assembly.GetName().Name}";
                row.tooltip = type.FullName;
            }
            else if (item is CommandInfo command)
            {
                name.text = command.Name;
                signature.text = command.ArgumentSignature;
                info.text = command.TargetDescription + (command.Description.Length > 0 ? $" · {command.Description}" : string.Empty);
                row.tooltip = command.Description;
            }
        }

        #endregion

        #region Selection

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            object item = selection.FirstOrDefault();
            _details.text = item switch
            {
                CommandInfo command => DescribeCommand(command),
                Type type => $"{type.FullName}\nAssembly: {type.Assembly.GetName().Name}\nPrefix: {DescribePrefix(type)}",
                _ => string.Empty
            };
        }

        private static void OnItemsChosen(IEnumerable<object> items)
        {
            switch (items.FirstOrDefault())
            {
                case CommandInfo command:
                    CommandSourceLocator.TryOpen(command.Member);
                    break;
                case Type type:
                    CommandInfo first = CommandRegistry.Default.Commands.FirstOrDefault(c => c.DeclaringType == type);
                    if (first != null) CommandSourceLocator.TryOpen(first.Member);
                    break;
            }
        }

        private static string DescribeCommand(CommandInfo command)
        {
            string access = command.Kind == CommandKind.Method ? "call" : command.CanWrite ? (command.CanRead ? "get / set" : "set") : "get";
            string target = command.IsStatic ? "Static" : $"Instance, MonoTargetType.{command.MonoTarget} of the active {TypeNames.GetFriendlyName(command.DeclaringType)} objects";
            return $"{command.Signature}\n" +
                   (command.Description.Length > 0 ? $"{command.Description}\n" : string.Empty) +
                   $"\nKind: {command.Kind} ({access})\n" +
                   $"Target: {target}\n" +
                   $"Declared in: {command.DeclaringType.FullName}.{command.Member.Name}\n" +
                   $"Assembly: {command.DeclaringType.Assembly.GetName().Name}";
        }

        private static string DescribePrefix(Type type)
        {
            string prefix = CommandDiscovery.GetPrefix(type);
            return prefix.Length > 0 ? prefix : "none";
        }

        #endregion
    }
}
