using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectWizard
{
    /// <summary>Inline styling helpers. Kept in code so the tool stays a single drag-and-drop folder.</summary>
    internal static class WizardStyles
    {
        private static bool Dark => EditorGUIUtility.isProSkin;

        public static Color Border => Dark ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.65f, 0.65f, 0.65f);
        public static Color Panel => Dark ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.84f, 0.84f, 0.84f);
        public static Color Muted => Dark ? new Color(0.62f, 0.62f, 0.62f) : new Color(0.35f, 0.35f, 0.35f);
        public static Color Accent => new Color(0.18f, 0.45f, 0.78f);

        public static void SetBorder(VisualElement e, float width, Color color)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = width;
            e.style.borderLeftWidth = e.style.borderRightWidth = width;
            e.style.borderTopColor = e.style.borderBottomColor = color;
            e.style.borderLeftColor = e.style.borderRightColor = color;
        }

        public static void SetRadius(VisualElement e, float radius)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = radius;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = radius;
        }

        public static void SetPadding(VisualElement e, float vertical, float horizontal)
        {
            e.style.paddingTop = e.style.paddingBottom = vertical;
            e.style.paddingLeft = e.style.paddingRight = horizontal;
        }

        public static void SetMargin(VisualElement e, float top, float bottom)
        {
            e.style.marginTop = top;
            e.style.marginBottom = bottom;
        }

        /// <summary>A bordered box with a bold title. Returns the box; add content to <paramref name="body"/>.</summary>
        public static VisualElement Section(string title, out VisualElement body)
        {
            var box = new VisualElement();
            SetBorder(box, 1, Border);
            SetRadius(box, 4);
            SetPadding(box, 6, 8);
            SetMargin(box, 0, 8);
            box.style.backgroundColor = Panel;

            var header = new Label(title);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 4;
            box.Add(header);

            body = new VisualElement();
            box.Add(body);
            return box;
        }

        public static Label Pill(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 9;
            label.style.color = Muted;
            SetBorder(label, 1, Muted);
            SetRadius(label, 8);
            SetPadding(label, 0, 6);
            label.style.marginLeft = 6;
            label.style.alignSelf = Align.Center;
            return label;
        }

        public static Button PrimaryButton(string text, System.Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 28;
            b.style.backgroundColor = Accent;
            b.style.color = Color.white;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            return b;
        }
    }
}
