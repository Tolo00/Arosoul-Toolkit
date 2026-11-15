using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWizard
{
    public sealed class ProjectWizardWindow : EditorWindow
    {
        private sealed class PackageRow
        {
            public PackageDefinition Def;
            public VisualElement Root;
            public Toggle Toggle;
            public Label InstalledLabel;
            public string SearchText;
            public bool Installed;
        }

        private sealed class CategoryGroup
        {
            public Label Header;
            public readonly List<PackageRow> Rows = new List<PackageRow>();
        }

        private readonly List<PackageRow> _rows = new List<PackageRow>();
        private readonly List<CategoryGroup> _groups = new List<CategoryGroup>();

        private VisualElement _controls;
        private Toggle _createFolders;
        private ToolbarSearchField _search;
        private Label _selectionCount;
        private Button _runButton;
        private Button _skipButton;
        private Button _cancelButton;
        private ProgressBar _progress;
        private ScrollView _logScroll;

        private int _renderedLogRevision = -1;
        private bool _wasRunning;

        [MenuItem("Tools/Project Wizard")]
        public static void Open()
        {
            var window = GetWindow<ProjectWizardWindow>("Project Wizard");
            window.minSize = new Vector2(480, 640);
            window.Show();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Build
        // ─────────────────────────────────────────────────────────────────

        public void CreateGUI()
        {
            _rows.Clear();
            _groups.Clear();

            var root = rootVisualElement;
            root.style.flexGrow = 1;
            WizardStyles.SetPadding(root, 8, 8);

            var title = new Label("Project Wizard");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            root.Add(title);

            // Everything the user can change is inside _controls so one call disables it while a run is active.
            _controls = new VisualElement();
            _controls.style.flexGrow = 1;
            root.Add(_controls);

            _controls.Add(BuildFolderSection());
            _controls.Add(BuildPackageSection());

            root.Add(BuildFooter());
            root.Add(BuildLogSection());

            InstallOrchestrator.Changed -= Refresh;
            InstallOrchestrator.Changed += Refresh;
            WizardLog.Changed -= Refresh;
            WizardLog.Changed += Refresh;

            RefreshInstalled(applyDefaults: true);
            UpdateSelectionCount();
            Refresh();
        }

        private void OnDisable()
        {
            InstallOrchestrator.Changed -= Refresh;
            WizardLog.Changed -= Refresh;
        }

        private void OnFocus()
        {
            if (_rows.Count > 0 && !InstallOrchestrator.IsRunning)
                RefreshInstalled(applyDefaults: false);
        }

        // ── folders ──────────────────────────────────────────────────────

        private VisualElement BuildFolderSection()
        {
            var section = WizardStyles.Section("Folders", out var body);

            _createFolders = new Toggle { text = "Create folders and starter files", value = true };
            body.Add(_createFolders);

            var preview = new Foldout { text = "Preview", value = false };
            foreach (var folder in WizardConfig.Folders)
                preview.Add(MutedLabel(folder));
            foreach (var file in WizardConfig.Files)
                preview.Add(MutedLabel(file.Path));
            body.Add(preview);

            var now = new Button(FolderSetup.Run) { text = "Create folders now" };
            now.style.marginTop = 4;
            body.Add(now);

            return section;
        }

        // ── packages ─────────────────────────────────────────────────────

        private VisualElement BuildPackageSection()
        {
            var section = WizardStyles.Section("Packages", out var body);
            section.style.flexGrow = 1;
            body.style.flexGrow = 1;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;
            toolbar.style.marginBottom = 4;

            _search = new ToolbarSearchField();
            _search.style.flexGrow = 1;
            _search.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(_search);

            toolbar.Add(new Button(() => SetVisible(true)) { text = "All" });
            toolbar.Add(new Button(() => SetVisible(false)) { text = "None" });
            body.Add(toolbar);

            var list = new ScrollView(ScrollViewMode.Vertical);
            list.style.flexGrow = 1;
            list.style.minHeight = 160;
            body.Add(list);

            foreach (var byCategory in WizardConfig.Packages.Where(p => !p.Hidden)
                         .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "Other" : p.Category))
            {
                var group = new CategoryGroup { Header = new Label(byCategory.Key) };
                group.Header.style.unityFontStyleAndWeight = FontStyle.Bold;
                group.Header.style.marginTop = 6;
                list.Add(group.Header);

                foreach (var def in byCategory)
                {
                    var row = BuildRow(def);
                    group.Rows.Add(row);
                    _rows.Add(row);
                    list.Add(row.Root);
                }

                _groups.Add(group);
            }

            _selectionCount = MutedLabel("");
            _selectionCount.style.marginTop = 4;
            body.Add(_selectionCount);

            return section;
        }

        private PackageRow BuildRow(PackageDefinition def)
        {
            var row = new PackageRow { Def = def };

            row.Root = new VisualElement();
            row.Root.style.marginLeft = 4;
            row.Root.style.marginBottom = 2;

            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;
            row.Root.Add(line);

            row.Toggle = new Toggle { text = def.Name };
            row.Toggle.style.flexGrow = 1;
            row.Toggle.RegisterValueChangedCallback(_ => UpdateSelectionCount());
            line.Add(row.Toggle);

            row.InstalledLabel = WizardStyles.Pill("installed");
            row.InstalledLabel.style.display = DisplayStyle.None;
            line.Add(row.InstalledLabel);
            line.Add(WizardStyles.Pill(def.SourceTag));

            if (!string.IsNullOrEmpty(def.Description))
            {
                var desc = MutedLabel(def.Description);
                desc.style.marginLeft = 20;
                row.Root.Add(desc);
            }

            row.SearchText = $"{def.Name} {def.Category} {def.Description} {def.SourceTag}".ToLowerInvariant();
            return row;
        }

        private void ApplyFilter()
        {
            var tokens = (_search.value ?? "")
                .ToLowerInvariant()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var group in _groups)
            {
                var anyVisible = false;
                foreach (var row in group.Rows)
                {
                    var visible = tokens.All(t => row.SearchText.Contains(t));
                    row.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    anyVisible |= visible;
                }

                group.Header.style.display = anyVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Checks or unchecks every package that is currently shown (respects the search filter).</summary>
        private void SetVisible(bool value)
        {
            foreach (var row in _rows)
            {
                if (row.Installed) continue;
                if (row.Root.resolvedStyle.display == DisplayStyle.None) continue;
                row.Toggle.SetValueWithoutNotify(value);
            }

            UpdateSelectionCount();
        }

        private void RefreshInstalled(bool applyDefaults)
        {
            foreach (var row in _rows)
            {
                row.Installed = PackageLookup.IsInstalled(row.Def);
                row.InstalledLabel.style.display = row.Installed ? DisplayStyle.Flex : DisplayStyle.None;
                row.Toggle.SetEnabled(!row.Installed);

                if (row.Installed)
                    row.Toggle.SetValueWithoutNotify(false);
                else if (applyDefaults)
                    row.Toggle.SetValueWithoutNotify(row.Def.SelectedByDefault);
            }

            UpdateSelectionCount();
        }

        private void UpdateSelectionCount()
        {
            if (_selectionCount == null) return;
            var count = _rows.Count(r => r.Toggle.value && !r.Installed);
            _selectionCount.text = $"{count} selected";
        }

        // ── footer ───────────────────────────────────────────────────────

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            WizardStyles.SetMargin(footer, 0, 8);

            _progress = new ProgressBar { lowValue = 0, highValue = 100 };
            _progress.style.display = DisplayStyle.None;
            _progress.style.marginBottom = 4;
            footer.Add(_progress);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;

            _runButton = WizardStyles.PrimaryButton("Install Selected", OnRunClicked);
            _runButton.style.flexGrow = 1;
            buttons.Add(_runButton);

            _skipButton = new Button(InstallOrchestrator.SkipCurrent) { text = "Skip current" };
            _skipButton.style.height = 28;
            _skipButton.style.display = DisplayStyle.None;
            buttons.Add(_skipButton);

            _cancelButton = new Button(InstallOrchestrator.Cancel) { text = "Cancel" };
            _cancelButton.style.height = 28;
            _cancelButton.style.display = DisplayStyle.None;
            buttons.Add(_cancelButton);

            footer.Add(buttons);

            return footer;
        }

        private void OnRunClicked()
        {
            if (InstallOrchestrator.IsRunning) return;

            var selected = _rows
                .Where(r => r.Toggle.value && !r.Installed)
                .Select(r => r.Def)
                .ToList();

            if (!_createFolders.value && selected.Count == 0)
            {
                WizardLog.Warn("Nothing selected.");
                return;
            }

            if (_createFolders.value)
                FolderSetup.Run();

            if (selected.Count == 0)
                return;

            var plan = InstallPlan.Build(selected);
            if (plan.stages.Count == 0)
            {
                WizardLog.Info("Everything selected is already installed.");
                return;
            }

            InstallOrchestrator.Start(plan);
        }

        // ── log ──────────────────────────────────────────────────────────

        private VisualElement BuildLogSection()
        {
            var section = WizardStyles.Section("Log", out var body);
            section.style.marginBottom = 0;

            _logScroll = new ScrollView(ScrollViewMode.Vertical);
            _logScroll.style.height = 150;
            body.Add(_logScroll);

            var clear = new Button(WizardLog.Clear) { text = "Clear log" };
            clear.style.marginTop = 4;
            body.Add(clear);

            return section;
        }

        private void RenderLog()
        {
            if (_logScroll == null || _renderedLogRevision == WizardLog.Revision) return;
            _renderedLogRevision = WizardLog.Revision;

            _logScroll.Clear();
            foreach (var line in WizardLog.Lines)
            {
                var label = new Label(line.text);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.fontSize = 11;
                label.selection.isSelectable = true;

                switch ((LogLevel)line.level)
                {
                    case LogLevel.Success: label.style.color = new Color(0.4f, 0.8f, 0.5f); break;
                    case LogLevel.Warning: label.style.color = new Color(0.95f, 0.7f, 0.2f); break;
                    case LogLevel.Error:   label.style.color = new Color(0.95f, 0.35f, 0.35f); break;
                }

                _logScroll.Add(label);
            }

            // scroll to the newest line once layout has run
            _logScroll.schedule.Execute(() =>
                _logScroll.verticalScroller.value = _logScroll.verticalScroller.highValue).ExecuteLater(20);
        }

        // ─────────────────────────────────────────────────────────────────
        //  State → UI
        // ─────────────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_runButton == null) return; // window not built yet

            var running = InstallOrchestrator.IsRunning;
            var state = InstallOrchestrator.State;

            _controls.SetEnabled(!running);
            _runButton.SetEnabled(!running);
            _cancelButton.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
            _skipButton.style.display =
                running && state.Current != null && state.Current.kind == StageKind.AssetStore
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            _progress.style.display = running ? DisplayStyle.Flex : DisplayStyle.None;
            if (running)
            {
                _progress.value = state.stages.Count == 0 ? 0 : 100f * state.index / state.stages.Count;
                _progress.title = InstallOrchestrator.Describe();
            }

            if (_wasRunning && !running)
                RefreshInstalled(applyDefaults: false);
            _wasRunning = running;

            RenderLog();
        }

        private static Label MutedLabel(string text)
        {
            var label = new Label(text);
            label.style.color = WizardStyles.Muted;
            label.style.fontSize = 10;
            return label;
        }
    }
}
