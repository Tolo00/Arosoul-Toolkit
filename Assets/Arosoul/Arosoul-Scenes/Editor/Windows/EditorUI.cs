using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Small helpers for building editor UI in code.
    /// </summary>
    internal static class EditorUI
    {
        public static Label Header(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 13;
            label.style.marginTop = 10;
            label.style.marginBottom = 4;
            return label;
        }

        public static VisualElement Section(string title)
        {
            var section = new VisualElement();
            section.style.marginLeft = 8;
            section.style.marginRight = 8;
            if (!string.IsNullOrEmpty(title)) section.Add(Header(title));
            return section;
        }

        public static VisualElement Card()
        {
            var card = new VisualElement();
            card.style.marginBottom = 4;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.paddingTop = 4;
            card.style.paddingBottom = 4;
            card.style.backgroundColor = new Color(0f, 0f, 0f, 0.12f);
            card.style.borderTopLeftRadius = 3;
            card.style.borderTopRightRadius = 3;
            card.style.borderBottomLeftRadius = 3;
            card.style.borderBottomRightRadius = 3;
            return card;
        }

        public static Label Muted(string text)
        {
            var label = new Label(text);
            label.style.opacity = 0.65f;
            return label;
        }

        public static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        public static HelpBox Issue(Arosoul.Scenes.Editor.ValidationIssue issue)
        {
            HelpBoxMessageType type = issue.Severity == ValidationSeverity.Error ? HelpBoxMessageType.Error
                : issue.Severity == ValidationSeverity.Warning ? HelpBoxMessageType.Warning
                : HelpBoxMessageType.Info;
            return new HelpBox(issue.Message, type);
        }
    }
}
