using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Edits the console settings. Every change is saved and baked into the runtime settings.
    /// </summary>
    public sealed class CommandsSettingsWindow : EditorWindow
    {
        private SerializedObject _serializedSettings;
        private VisualElement _diagnostics;
        private bool _commitScheduled;

        [MenuItem("Tools/Arosoul Commands/Settings", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<CommandsSettingsWindow>();
            window.titleContent = new GUIContent("Commands Settings");
            window.minSize = new Vector2(360, 400);
        }

        /// <summary>
        /// Rebuild open windows after the settings were changed from code.
        /// </summary>
        internal static void RefreshOpenWindows()
        {
            foreach (CommandsSettingsWindow window in Resources.FindObjectsOfTypeAll<CommandsSettingsWindow>())
                window.Rebuild();
        }

        private void CreateGUI()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            rootVisualElement.Clear();
            rootVisualElement.Unbind();
            _serializedSettings = CommandsSettings.instance.CreateSerializedObject();

            var scroll = new ScrollView();
            scroll.contentContainer.style.paddingLeft = 10;
            scroll.contentContainer.style.paddingRight = 10;
            scroll.contentContainer.style.paddingTop = 6;
            scroll.contentContainer.style.paddingBottom = 10;
            rootVisualElement.Add(scroll);

            scroll.Add(CreateToolbar());

            SerializedProperty data = _serializedSettings.FindProperty("_data");
            SerializedProperty iterator = data.Copy();
            SerializedProperty end = data.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                scroll.Add(new PropertyField(iterator.Copy()));
                enterChildren = false;
            }

            var diagnosticsHeader = new Label("Validation");
            diagnosticsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            diagnosticsHeader.style.marginTop = 12;
            scroll.Add(diagnosticsHeader);

            _diagnostics = new VisualElement();
            scroll.Add(_diagnostics);
            RefreshDiagnostics();

            rootVisualElement.Bind(_serializedSettings);
            rootVisualElement.TrackSerializedObjectValue(_serializedSettings, _ => ScheduleCommit());
        }

        private VisualElement CreateToolbar()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 6;

            row.Add(new Button(() => Selection.activeGameObject = CommandsConsoleCreator.AddConsoleToActiveScene())
            {
                text = "Add Console To Scene",
                tooltip = "Add the Commands Console prefab to the active scene, or select the console that is already there."
            });
            row.Add(new Button(CommandsDebuggerWindow.Open) { text = "Open Debugger" });
            return row;
        }

        private void ScheduleCommit()
        {
            if (_commitScheduled) return;

            _commitScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _commitScheduled = false;
                CommandsSettings.instance.SaveSettings();
                CommandsSettingsBaker.Bake();
                RefreshDiagnostics();
            };
        }

        private void RefreshDiagnostics()
        {
            if (_diagnostics == null) return;

            _diagnostics.Clear();
            string[] diagnostics = CommandsAgentTools.Validate();
            if (diagnostics.Length == 0)
            {
                _diagnostics.Add(new HelpBox("No problems found.", HelpBoxMessageType.Info));
                return;
            }

            foreach (string diagnostic in diagnostics)
            {
                bool isError = diagnostic.StartsWith("ERROR");
                _diagnostics.Add(new HelpBox(diagnostic, isError ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning));
            }
        }
    }
}
