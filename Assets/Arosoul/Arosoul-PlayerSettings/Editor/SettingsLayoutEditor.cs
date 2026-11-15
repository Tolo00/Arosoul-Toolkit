using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings.EditorTools {

    [CustomEditor(typeof(SettingsLayout))]
    public sealed class SettingsLayoutEditor : Editor {
        static readonly string[] Properties = { "title", "tabOrientation", "applyMode", "showResetButton", "showCloseButton", "theme" };

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            foreach (string name in Properties) root.Add(new PropertyField(serializedObject.FindProperty(name)));

            var layout = (SettingsLayout)target;
            var summary = new HelpBox(string.Empty, HelpBoxMessageType.None);
            summary.style.marginTop = 6;
            root.Add(summary);

            var edit = new Button(() => SettingsLayoutWindow.Open(layout)) { text = "Edit Categories" };
            edit.style.marginTop = 6;
            edit.style.height = 26;
            root.Add(edit);

            void Refresh() {
                int entries = 0;
                foreach (var c in layout.Categories) entries += c.entries.Count;
                summary.text = $"{layout.Categories.Count} categories, {entries} settings";
            }
            Refresh();
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            return root;
        }
    }

}
