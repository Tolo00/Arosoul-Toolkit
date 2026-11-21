using UnityEngine;
using UnityEngine.UIElements;

#if PLAYERSETTINGS_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

namespace Arosoul.PlayerSettings {

    public static class SettingsNavigation {
        /// <summary>Added to every row. Focus highlighting and hover-focus work on these.</summary>
        public const string RowClass = "settings-row";
        public const string FocusedRowClass = "settings-row--focused";
        /// <summary>Set on the menu root while keyboard/controller is the active input. Focus highlights only show then.</summary>
        public const string NavActiveClass = "settings-nav-active";

        static int captureCount;
        static float blockedUntil;

        /// <summary>True while something (e.g. key rebinding) listens for raw input. Menu navigation ignores input then.</summary>
        public static bool IsBlocked => captureCount > 0 || Time.unscaledTime < blockedUntil;

        public static void BeginCapture() => captureCount++;

        public static void EndCapture(float cooldown = 0.25f) {
            captureCount = Mathf.Max(0, captureCount - 1);
            blockedUntil = Time.unscaledTime + cooldown;
        }

        /// <summary>Marks the top element of a template instance as a row (call for every row you add).</summary>
        public static void MarkRow(VisualElement templateRoot) =>
            (templateRoot.childCount == 1 ? templateRoot[0] : templateRoot).AddToClassList(RowClass);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { captureCount = 0; blockedUntil = 0f; }

        /// <summary>Handles a navigation event completely: nothing else reacts to it and Unity doesn't move focus.</summary>
        public static void Consume(EventBase evt) {
            evt.StopPropagation();
            (evt.target as VisualElement)?.panel?.focusController?.IgnoreEvent(evt);
        }
    }

    internal static class MenuInput {
        /// <summary>-1 / +1 when a shoulder button was pressed this frame.</summary>
        public static int TabDirection() {
#if PLAYERSETTINGS_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return 0;
            if (pad.leftShoulder.wasPressedThisFrame) return -1;
            if (pad.rightShoulder.wasPressedThisFrame) return 1;
#endif
            return 0;
        }
    }

}