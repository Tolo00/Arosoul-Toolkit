using System;
using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Events raised by Scenes. All subscriptions are cleared when entering play mode, so they work with domain reload disabled.
    /// </summary>
    public static class SceneEvents
    {
        public static event Action<SceneOperation> OperationQueued;
        public static event Action<SceneOperation> OperationStarted;
        public static event Action<SceneOperation> OperationCompleted;
        public static event Action<SceneOperation> OperationFailed;
        public static event Action<SceneOperation, SceneOperationPhase> PhaseChanged;
        public static event Action<SceneOperation, float> ProgressChanged;
        public static event Action<LoadedSceneInfo> SceneLoaded;
        public static event Action<LoadedSceneInfo> SceneUnloading;
        public static event Action<LoadedSceneInfo> SceneUnloaded;

        internal static void RaiseOperationQueued(SceneOperation op) => OperationQueued?.Invoke(op);
        internal static void RaiseOperationStarted(SceneOperation op) => OperationStarted?.Invoke(op);
        internal static void RaiseOperationCompleted(SceneOperation op) => OperationCompleted?.Invoke(op);
        internal static void RaiseOperationFailed(SceneOperation op) => OperationFailed?.Invoke(op);
        internal static void RaisePhaseChanged(SceneOperation op, SceneOperationPhase phase) => PhaseChanged?.Invoke(op, phase);
        internal static void RaiseProgressChanged(SceneOperation op, float progress) => ProgressChanged?.Invoke(op, progress);
        internal static void RaiseSceneLoaded(LoadedSceneInfo info) => SceneLoaded?.Invoke(info);
        internal static void RaiseSceneUnloading(LoadedSceneInfo info) => SceneUnloading?.Invoke(info);
        internal static void RaiseSceneUnloaded(LoadedSceneInfo info) => SceneUnloaded?.Invoke(info);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OperationQueued = null;
            OperationStarted = null;
            OperationCompleted = null;
            OperationFailed = null;
            PhaseChanged = null;
            ProgressChanged = null;
            SceneLoaded = null;
            SceneUnloading = null;
            SceneUnloaded = null;
        }
    }
}
