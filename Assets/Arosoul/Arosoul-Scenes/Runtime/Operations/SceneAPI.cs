using System.Collections.Generic;
using Arosoul.Transitions;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Entry point of Scenes.
    ///
    /// await SceneAPI.Load(SlotIds.Main, SceneIds.Gameplay)
    ///     .WithTransition(TransitionIds.Fade)
    ///     .PerformAsync();
    /// </summary>
    public static class SceneAPI
    {
        /// <summary>
        /// Start building an operation.
        /// </summary>
        public static SceneOperation NewOperation() => new SceneOperation();

        public static SceneOperation Load(SlotId slot, SceneRef scene, bool setActive = false, bool reload = false)
        {
            return new SceneOperation().Load(slot, scene, setActive, reload);
        }

        public static SceneOperation Unload(SlotId slot)
        {
            return new SceneOperation().Unload(slot);
        }

        /// <summary>
        /// True while an operation is running or queued.
        /// </summary>
        public static bool IsBusy => SceneManager.HasInstance && SceneManager.Inst.IsBusy;

        /// <summary>
        /// All scenes currently loaded by Scenes, in load order.
        /// </summary>
        public static IReadOnlyList<LoadedSceneInfo> LoadedScenes
        {
            get
            {
                return SceneManager.HasInstance ? SceneManager.Inst.LoadedScenes : System.Array.Empty<LoadedSceneInfo>();
            }
        }

        public static bool IsLoaded(SceneRef scene)
        {
            return SceneManager.HasInstance && SceneManager.Inst.TryGetLoaded(scene.Guid, out _);
        }

        public static bool IsSlotOccupied(SlotId slot)
        {
            return SceneManager.HasInstance && SceneManager.Inst.TryGetSlotScene(slot, out _);
        }

        public static bool TryGetSceneInSlot(SlotId slot, out LoadedSceneInfo info)
        {
            info = null;
            return SceneManager.HasInstance && SceneManager.Inst.TryGetSlotScene(slot, out info);
        }
    }
}
