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

        /// <summary>
        /// Start an operation that loads a scene into a slot, see <see cref="SceneOperation.Load"/>.
        /// </summary>
        public static SceneOperation Load(SlotId slot, SceneRef scene, bool setActive = false, bool reload = false)
        {
            return new SceneOperation().Load(slot, scene, setActive, reload);
        }

        /// <summary>
        /// Start an operation that empties a slot, see <see cref="SceneOperation.Unload"/>.
        /// </summary>
        public static SceneOperation Unload(SlotId slot)
        {
            return new SceneOperation().Unload(slot);
        }

        /// <summary>
        /// Replace the backend that loads and unloads Unity scenes, see <see cref="ISceneLoader"/>.
        /// Call it before the first operation, for example from a RuntimeInitializeOnLoadMethod(BeforeSceneLoad) method.
        /// </summary>
        /// <returns>False if operations are queued or running, the loader is then not replaced.</returns>
        public static bool SetLoader(ISceneLoader loader)
        {
            return SceneManager.Inst.SetLoader(loader);
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
