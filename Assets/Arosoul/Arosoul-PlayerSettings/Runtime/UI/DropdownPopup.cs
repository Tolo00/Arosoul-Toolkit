using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>Custom dropdown implementation to give the system more control.</summary>
    internal static class DropdownPopup {
        static VisualElement overlay;
        static VisualElement anchor;

        public static bool IsOpen => overlay != null;
        public static VisualElement Overlay => overlay;
        public static bool Contains(VisualElement e) => overlay != null && e != null && overlay.Contains(e);

        public static void Open(VisualElement anchorElement, IReadOnlyList<string> labels, int selected, Action<int> chosen) {
            Close(false);
            VisualElement menuRoot = anchorElement;
            while (menuRoot != null && !menuRoot.ClassListContains("settings-root")) menuRoot = menuRoot.parent;
            if (menuRoot == null || labels.Count == 0) return;

            anchor = anchorElement;
            overlay = new VisualElement();
            overlay.AddToClassList("settings-dropdown-overlay");
            overlay.RegisterCallback<PointerDownEvent>(e => { // click outside the list
                if (e.target != overlay) return;
                Close(true);
                e.StopPropagation();
            });

            var list = new ScrollView
            {
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            list.AddToClassList("settings-dropdown-list");
            overlay.Add(list);

            var bounds = anchorElement.worldBound;
            Vector2 origin = menuRoot.WorldToLocal(bounds.position);
            float height = Mathf.Min(260f, labels.Count * 34f + 8f);
            float top = origin.y + bounds.height;
            if (top + height > menuRoot.layout.height) top = Mathf.Max(0f, origin.y - height); // no room below: open upwards
            list.style.left = origin.x;
            list.style.top = top;
            list.style.width = bounds.width;

            for (int i = 0; i < labels.Count; i++) {
                int index = i;
                var item = new Button(() => { Close(true); chosen(index); }) { text = labels[i] };
                item.AddToClassList("settings-dropdown-item");
                item.EnableInClassList("settings-dropdown-item--selected", i == selected);
                list.Add(item);
            }

            menuRoot.Add(overlay);
            list.contentContainer[Mathf.Clamp(selected, 0, labels.Count - 1)].Focus();
        }

        public static void Close(bool refocusAnchor) {
            if (overlay == null) return;
            var target = anchor;
            overlay.RemoveFromHierarchy();
            overlay = null;
            anchor = null;
            if (refocusAnchor) target?.Focus();
        }
    }

}