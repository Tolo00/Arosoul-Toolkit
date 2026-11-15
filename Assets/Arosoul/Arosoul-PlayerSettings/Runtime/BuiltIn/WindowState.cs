using UnityEngine;

namespace Arosoul.PlayerSettings {

    /// <summary>Resolution and display mode have to be applied together, this keeps the two halves in sync.</summary>
    internal static class WindowState {
        static bool initialized;
        static int width, height;
        static FullScreenMode mode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => initialized = false;

        static void EnsureInitialized() {
            if (initialized) return;
            width = Screen.width;
            height = Screen.height;
            mode = Screen.fullScreenMode;
            initialized = true;
        }

        public static void SetSize(int w, int h) {
            EnsureInitialized();
            width = w;
            height = h;
            Flush();
        }

        public static void SetMode(FullScreenMode newMode) {
            EnsureInitialized();
            mode = newMode;
            Flush();
        }

        static void Flush() => Screen.SetResolution(width, height, mode);
    }

}
