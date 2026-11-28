using System.Collections.Generic;
using System.Text;
using Arosoul.Transitions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Live view of Scenes in play mode: loaded scenes and why they are loaded, slots, the running and queued operations,
    /// the active transition, input blocking and a log of recent events.
    /// </summary>
    public class SceneDebuggerWindow : EditorWindow
    {
        private VisualElement _content;

        [MenuItem("Tools/Arosoul/Scenes/Scene Debugger", priority = 107)]
        public static void Open()
        {
            var window = GetWindow<SceneDebuggerWindow>();
            window.titleContent = new GUIContent("Scene Debugger");
            window.minSize = new Vector2(420, 360);
        }

        private void CreateGUI()
        {
            var scroll = new ScrollView();
            _content = scroll;
            rootVisualElement.Add(scroll);

            Refresh();
            rootVisualElement.schedule.Execute(Refresh).Every(150);
        }

        private void Refresh()
        {
            _content.Clear();

            if (!EditorApplication.isPlaying)
            {
                _content.Add(new HelpBox("Enter play mode to inspect Scenes.", HelpBoxMessageType.Info));
                AddLog(_content);
                return;
            }

            if (!SceneManager.HasInstance)
            {
                _content.Add(new HelpBox("Scenes is not running. Create a scene catalog in Tools > Arosoul > Scenes > Scene Config.", HelpBoxMessageType.Warning));
                AddLog(_content);
                return;
            }

            SceneManager manager = SceneManager.Inst;
            AddOperations(manager);
            AddTransition();
            AddLoadedScenes(manager);
            AddSlots(manager);
            AddLog(_content);
        }

        private void AddOperations(SceneManager manager)
        {
            var section = EditorUI.Section("Operations");
            SceneOperation current = manager.CurrentOperation;

            if (current == null)
            {
                section.Add(EditorUI.Muted("Idle"));
            }
            else
            {
                var card = EditorUI.Card();
                card.Add(new Label(current.Describe()));
                card.Add(EditorUI.Muted($"{current.State} - {current.Phase}"));
                var bar = new ProgressBar { value = current.Progress * 100f, title = $"{current.Progress * 100f:0}%" };
                card.Add(bar);
                section.Add(card);
            }

            IReadOnlyList<SceneOperation> queued = manager.GetQueuedOperations();
            if (queued.Count > 0)
            {
                section.Add(EditorUI.Muted($"Queued ({queued.Count})"));
                foreach (var op in queued)
                    section.Add(new Label("  " + op.Describe()));
            }
            _content.Add(section);
        }

        private void AddTransition()
        {
            var section = EditorUI.Section("Transition and Input");

            TransitionHandle handle = TransitionAPI.GetActive();
            if (handle == null)
            {
                section.Add(EditorUI.Muted("No active transition"));
            }
            else
            {
                string text = $"{handle.Parameters.Id} - {handle.Phase}, priority {handle.Parameters.Priority}";
                if (!handle.IsReleased) text += ", held";
                section.Add(new Label(text));
                if (handle.Progress > 0f || !string.IsNullOrEmpty(handle.Message))
                    section.Add(EditorUI.Muted($"{handle.Progress * 100f:0}% {handle.Message}"));
            }

            if (InputBlocker.IsBlocked)
            {
                var reasons = new StringBuilder("Input blocked: ");
                reasons.Append(string.Join("; ", InputBlocker.GetReasons()));
                section.Add(new Label(reasons.ToString()) { style = { color = new Color(1f, 0.8f, 0.4f), whiteSpace = WhiteSpace.Normal } });
            }
            else
            {
                section.Add(EditorUI.Muted("Input not blocked"));
            }
            _content.Add(section);
        }

        private void AddLoadedScenes(SceneManager manager)
        {
            var section = EditorUI.Section($"Loaded Scenes ({manager.LoadedScenes.Count})");
            SceneGraph graph = manager.Catalog != null ? manager.Catalog.Graph : null;

            var loadedGuids = new HashSet<string>();
            foreach (var info in manager.LoadedScenes) loadedGuids.Add(info.Guid);

            foreach (LoadedSceneInfo info in manager.LoadedScenes)
            {
                var card = EditorUI.Card();
                var header = EditorUI.Row();

                bool isActive = info.Scene.IsValid() && info.Scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var name = new Label(info.Name + (isActive ? "  (active)" : ""));
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.flexGrow = 1;
                header.Add(name);
                header.Add(EditorUI.Muted(RoleOf(info)));
                card.Add(header);

                var details = new StringBuilder();
                details.Append($"load {info.LoadDuration:0.00}s");
                if (info.InitializationDuration > 0f) details.Append($", init {info.InitializationDuration:0.00}s");

                if (graph != null)
                {
                    var needed = new List<string>();
                    foreach (string dependent in graph.GetDependents(info.Guid))
                        if (loadedGuids.Contains(dependent)) needed.Add(manager.Catalog.GetName(dependent));
                    if (needed.Count > 0) details.Append(", needed by ").Append(string.Join(", ", needed));
                }
                card.Add(EditorUI.Muted(details.ToString()));
                section.Add(card);
            }
            _content.Add(section);
        }

        private static string RoleOf(LoadedSceneInfo info)
        {
            if (info.IsCore) return "Core";
            if (!info.Slot.IsNone) return $"Slot: {info.Slot}";
            return "Dependency";
        }

        private void AddSlots(SceneManager manager)
        {
            var section = EditorUI.Section("Slots");
            SceneCatalog catalog = manager.Catalog;
            if (catalog == null)
            {
                _content.Add(section);
                return;
            }

            foreach (SlotDefinition slot in catalog.Data.Slots)
            {
                string content = "(empty)";
                if (manager.Slots.TryGetValue(slot.Id, out string guid))
                    content = catalog.GetName(guid);

                var row = EditorUI.Row();
                var key = new Label(slot.Key) { style = { width = 140 } };
                row.Add(key);
                row.Add(content == "(empty)" ? EditorUI.Muted(content) : new Label(content));
                section.Add(row);
            }
            _content.Add(section);
        }

        private static void AddLog(VisualElement parent)
        {
            var section = EditorUI.Section("Log");
            var clear = new Button(SceneEventLog.Clear) { text = "Clear" };
            clear.style.alignSelf = Align.FlexStart;
            section.Add(clear);

            IReadOnlyList<SceneEventLog.Entry> entries = SceneEventLog.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var row = EditorUI.Row();
                row.Add(new Label($"{entries[i].Time:0.00}") { style = { width = 56, opacity = 0.6f } });
                row.Add(new Label(entries[i].Message) { style = { whiteSpace = WhiteSpace.Normal, flexShrink = 1 } });
                section.Add(row);
            }
            parent.Add(section);
        }
    }
}
