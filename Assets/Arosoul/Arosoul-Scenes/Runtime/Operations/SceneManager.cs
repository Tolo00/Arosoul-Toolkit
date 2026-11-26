using System;
using System.Collections.Generic;
using Arosoul.Transitions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Runs scene operations. Created on demand, use <see cref="SceneAPI"/> to interact with it.
    ///
    /// The manager keeps a loaded scene as long as it is in a slot, is the core scene, or is a dependency of a kept scene.
    /// Operations are queued and executed one at a time:
    /// transition in, unload, load (dependencies first, each scene initialized before the next is loaded), transition out.
    /// </summary>
    public sealed class SceneManager : LazyLoaded<SceneManager>
    {
        private readonly Dictionary<string, LoadedSceneInfo> _loaded = new Dictionary<string, LoadedSceneInfo>();
        private readonly List<LoadedSceneInfo> _loadedOrder = new List<LoadedSceneInfo>();
        private readonly Dictionary<SlotId, string> _slots = new Dictionary<SlotId, string>();
        private readonly Queue<SceneOperation> _queue = new Queue<SceneOperation>();

        // Scenes loaded outside of an operation (the startup scene). Initialized by the next operation, after their dependencies.
        private readonly List<LoadedSceneInfo> _pendingInitialization = new List<LoadedSceneInfo>();

        private SceneCatalog _catalog;
        private ISceneLoader _loader;
        private SceneOperation _current;
        private bool _processing;

        public SceneCatalog Catalog => _catalog;
        public ISceneLoader Loader => _loader;
        public bool IsBusy => _processing;
        public SceneOperation CurrentOperation => _current;
        public int QueuedCount => _queue.Count;
        public IReadOnlyList<LoadedSceneInfo> LoadedScenes => _loadedOrder;
        public IReadOnlyDictionary<SlotId, string> Slots => _slots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearInstance();
        }

        protected override void OnSingletonEnable()
        {
            _catalog = SceneCatalog.Instance;
            _loader = new UnitySceneLoader();
        }

        /// <summary>
        /// Replace the backend that loads and unloads Unity scenes, for example to load scenes through Addressables.
        /// Only possible while no operation is queued or running. Scenes that are already loaded stay tracked and are unloaded
        /// through the new loader, so it must be able to unload scenes it did not load itself (such as the startup scene).
        /// </summary>
        /// <returns>True if the loader was replaced.</returns>
        public bool SetLoader(ISceneLoader loader)
        {
            if (loader == null) throw new ArgumentNullException(nameof(loader));
            if (_processing || _queue.Count > 0)
            {
                Debug.LogError("The scene loader can not be replaced while scene operations are queued or running.", this);
                return false;
            }

            _loader = loader;
            return true;
        }

        /// <summary>
        /// Replace the catalog and loader and forget all state. Used by tests.
        /// </summary>
        internal void Configure(SceneCatalog catalog, ISceneLoader loader)
        {
            _catalog = catalog;
            _loader = loader;
            _loaded.Clear();
            _loadedOrder.Clear();
            _slots.Clear();
            _queue.Clear();
            _pendingInitialization.Clear();
            _current = null;
        }

        public IReadOnlyList<SceneOperation> GetQueuedOperations()
        {
            return _queue.ToArray();
        }

        public bool TryGetLoaded(string guid, out LoadedSceneInfo info)
        {
            return _loaded.TryGetValue(guid, out info);
        }

        public bool TryGetSlotScene(SlotId slot, out LoadedSceneInfo info)
        {
            info = null;
            return _slots.TryGetValue(slot, out string guid) && _loaded.TryGetValue(guid, out info);
        }

        #region Queue

        internal void Enqueue(SceneOperation op)
        {
            if (op.State != SceneOperationState.Created)
            {
                Debug.LogWarning("A scene operation can only be performed once.");
                return;
            }

            op.MarkQueued();
            _queue.Enqueue(op);
            SceneEvents.RaiseOperationQueued(op);

            if (!_processing)
                ProcessQueueAsync();
        }

        private async void ProcessQueueAsync()
        {
            _processing = true;
            try
            {
                while (_queue.Count > 0)
                {
                    SceneOperation op = _queue.Dequeue();
                    if (op.State == SceneOperationState.Cancelled) continue;

                    _current = op;
                    await RunOperationAsync(op);
                    _current = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _current = null;
                _processing = false;
            }
        }

        #endregion

        #region Execution

        private async Awaitable RunOperationAsync(SceneOperation op)
        {
            Exception error = null;
            IDisposable inputBlock = null;
            TransitionHandle transition = null;

            op.MarkRunning();
            SceneEvents.RaiseOperationStarted(op);
            SceneEventLog.Add($"Started: {op.Describe()}");
            float startTime = Time.realtimeSinceStartup;

            try
            {
                SetPhase(op, SceneOperationPhase.Planning);
                if (_catalog == null)
                    throw new InvalidOperationException("No scene catalog found. Open Tools > Scene System > Scene Config to set up Scenes.");

                ScenePlan plan = BuildPlan(op, out string activeGuid);
                if (!plan.IsValid)
                    throw new InvalidOperationException(string.Join("\n", plan.Errors));

                if (op.BlockInput)
                    inputBlock = InputBlocker.Acquire($"Scene operation: {op.Describe()}");

                // Transition in
                if (op.Transition.HasValue)
                {
                    SetPhase(op, SceneOperationPhase.TransitionIn);
                    TransitionParameters parameters = op.Transition.Value.WithHoldUntilReleased(true);
                    if (parameters.Priority == 0)
                        parameters = parameters.WithPriority(SceneOperation.DefaultTransitionPriority);

                    transition = TransitionAPI.Play(parameters);
                    await transition.WaitForPhaseAsync(TransitionPhase.Hold);

                    // The transition was rejected or interrupted, continue without it.
                    if (transition.Phase == TransitionPhase.Cancelled)
                        transition = null;
                }

                // Unload
                SetPhase(op, SceneOperationPhase.Unloading);
                for (int i = 0; i < plan.ToUnload.Count; i++)
                    await UnloadSceneAsync(plan.ToUnload[i]);

                // Load
                SetPhase(op, SceneOperationPhase.Loading);
                for (int i = 0; i < plan.ToLoad.Count; i++)
                    await LoadSceneAsync(plan.ToLoad[i], plan, i, plan.ToLoad.Count, op, transition, activeGuid);

                // Now that their dependencies are loaded, initialize scenes that were loaded outside of an operation.
                while (_pendingInitialization.Count > 0)
                {
                    LoadedSceneInfo pending = _pendingInitialization[0];
                    _pendingInitialization.RemoveAt(0);
                    if (_loaded.ContainsKey(pending.Guid))
                        await InitializeSceneAsync(pending, op);
                }

                ApplySlots(plan);

                if (activeGuid != null && _loaded.TryGetValue(activeGuid, out LoadedSceneInfo active))
                    _loader.SetActive(active.Scene);

                if (op.UnloadUnusedAssets)
                {
                    SetPhase(op, SceneOperationPhase.UnloadingAssets);
                    await _loader.UnloadUnusedAssetsAsync();
                }

                ReportProgress(op, transition, 1f, "Done");
            }
            catch (Exception e)
            {
                error = e;
                RebuildSlotsFromLoaded();
            }

            // Always leave the transition and release input, also when the operation failed.
            if (transition != null && !transition.IsFinished)
            {
                SetPhase(op, SceneOperationPhase.TransitionOut);
                transition.Release();
                await transition.WaitForPhaseAsync(TransitionPhase.Completed);
            }
            inputBlock?.Dispose();

            SetPhase(op, SceneOperationPhase.Done);
            float duration = Time.realtimeSinceStartup - startTime;

            if (error != null)
            {
                op.Fail(error);
                SceneEventLog.Add($"Failed: {op.Describe()} ({error.Message})");
                Debug.LogError($"Scene operation failed ({op.Describe()}): {error.Message}", this);
                SceneEvents.RaiseOperationFailed(op);
            }
            else
            {
                op.Complete();
                SceneEventLog.Add($"Completed: {op.Describe()} ({duration:0.00}s)");
                SceneEvents.RaiseOperationCompleted(op);
            }
        }

        private ScenePlan BuildPlan(SceneOperation op, out string activeGuid)
        {
            activeGuid = op.ActiveScene.IsValid ? op.ActiveScene.Guid : null;

            var loads = new List<SlotLoadRequest>();
            var unloads = new List<SlotId>(op.Unloads);

            for (int i = 0; i < op.Loads.Count; i++)
            {
                var request = op.Loads[i];
                loads.Add(new SlotLoadRequest(request.Slot, request.Scene.Guid, request.Reload));
            }

            return ScenePlanner.Plan(_catalog.Graph, _slots, _loaded.Keys, loads, unloads);
        }

        private async Awaitable UnloadSceneAsync(string guid)
        {
            if (!_loaded.TryGetValue(guid, out LoadedSceneInfo info)) return;

            SceneEventLog.Add($"Unloading '{info.Name}'");
            SceneEvents.RaiseSceneUnloading(info);
            await _loader.UnloadAsync(info.Scene, info.Path);

            _loaded.Remove(guid);
            _loadedOrder.Remove(info);
            SceneEvents.RaiseSceneUnloaded(info);
        }

        private async Awaitable LoadSceneAsync(string guid, ScenePlan plan, int index, int count, SceneOperation op, TransitionHandle transition, string activeGuid)
        {
            if (!_catalog.TryGetPath(guid, out string path))
                throw new InvalidOperationException($"No path found for scene '{guid}'. Rebuild the scene catalog in the Scene Config window.");

            string sceneName = _catalog.GetName(guid);
            SceneEventLog.Add($"Loading '{sceneName}'");
            float start = Time.realtimeSinceStartup;

            Scene scene = await _loader.LoadAdditiveAsync(path, p => ReportProgress(op, transition, (index + p) / count, $"Loading {sceneName}"));

            var info = new LoadedSceneInfo(guid, sceneName, path, scene)
            {
                IsCore = guid == _catalog.Graph.CoreGuid,
                LoadDuration = Time.realtimeSinceStartup - start
            };
            foreach (var kvp in plan.FinalSlots)
                if (kvp.Value == guid) info.Slot = kvp.Key;

            _loaded[guid] = info;
            _loadedOrder.Add(info);

            if (guid == activeGuid)
                _loader.SetActive(scene);

            SceneEvents.RaiseSceneLoaded(info);
            await InitializeSceneAsync(info, op);
        }

        // Readiness gating. The operation does not continue until every initializer in the scene has finished.
        private async Awaitable InitializeSceneAsync(LoadedSceneInfo info, SceneOperation op)
        {
            List<ISceneInitializer> initializers = _loader.GetInitializers(info.Scene, info.Path);
            if (initializers == null || initializers.Count == 0) return;

            SetPhase(op, SceneOperationPhase.Initializing);
            float start = Time.realtimeSinceStartup;
            var context = new SceneContext(info.Guid, info.Name, info.Slot, op.Payload);

            var groups = new SortedDictionary<int, List<ISceneInitializer>>();
            foreach (ISceneInitializer initializer in initializers)
            {
                int order = initializer.InitializationOrder;
                if (!groups.TryGetValue(order, out List<ISceneInitializer> group))
                    groups[order] = group = new List<ISceneInitializer>();
                group.Add(initializer);
            }

            Exception firstError = null;
            foreach (var group in groups.Values)
            {
                // Start everything in the group, then wait for all of it.
                var running = new List<Awaitable>(group.Count);
                foreach (ISceneInitializer initializer in group)
                {
                    try { running.Add(initializer.InitializeAsync(context)); }
                    catch (Exception e) { Debug.LogException(e); firstError ??= e; }
                }

                foreach (Awaitable awaitable in running)
                {
                    try { await awaitable; }
                    catch (Exception e) { Debug.LogException(e); firstError ??= e; }
                }
            }

            info.InitializationDuration = Time.realtimeSinceStartup - start;
            SceneEventLog.Add($"Initialized '{info.Name}' ({info.InitializationDuration:0.00}s)");

            if (firstError != null)
                throw new InvalidOperationException($"Initialization of scene '{info.Name}' failed: {firstError.Message}", firstError);

            SetPhase(op, SceneOperationPhase.Loading);
        }

        private static void SetPhase(SceneOperation op, SceneOperationPhase phase)
        {
            op.SetPhase(phase);
        }

        private static void ReportProgress(SceneOperation op, TransitionHandle transition, float progress, string message)
        {
            op.SetProgress(progress);
            if (transition != null && !transition.IsFinished)
                transition.SetProgress(progress, message);
        }

        private void ApplySlots(ScenePlan plan)
        {
            _slots.Clear();
            foreach (var kvp in plan.FinalSlots)
                _slots[kvp.Key] = kvp.Value;

            for (int i = 0; i < _loadedOrder.Count; i++)
            {
                LoadedSceneInfo info = _loadedOrder[i];
                info.Slot = SlotId.None;
            }
            foreach (var kvp in _slots)
                if (_loaded.TryGetValue(kvp.Value, out LoadedSceneInfo info))
                    info.Slot = kvp.Key;
        }

        private void RebuildSlotsFromLoaded()
        {
            _slots.Clear();
            for (int i = 0; i < _loadedOrder.Count; i++)
                if (!_loadedOrder[i].Slot.IsNone)
                    _slots[_loadedOrder[i].Slot] = _loadedOrder[i].Guid;
        }

        #endregion

        #region Startup

        /// <summary>
        /// Registers the scene that was loaded by Unity when the game started in the initial slot, and loads its missing dependencies (including core).
        /// The scene's initializers run after those dependencies are loaded.
        /// Used when the game starts without the play mode bootstrap, for example in builds.
        /// </summary>
        internal Awaitable AdoptInitialSceneAsync()
        {
            Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return AdoptSceneAsync(active, active.IsValid() ? active.path : null);
        }

        /// <summary>
        /// Registers an already loaded scene in the initial slot, then runs an empty operation that loads its missing dependencies
        /// and initializes it.
        /// </summary>
        internal async Awaitable AdoptSceneAsync(Scene scene, string path)
        {
            if (_catalog == null) return;

            SlotId initialSlot = _catalog.Data.InitialSlot;
            if (!initialSlot.IsNone && !string.IsNullOrEmpty(path) && _catalog.TryGetGuidByPath(path, out string guid) && !_loaded.ContainsKey(guid))
            {
                var info = new LoadedSceneInfo(guid, _catalog.GetName(guid), path, scene)
                {
                    Slot = initialSlot,
                    IsCore = guid == _catalog.Graph.CoreGuid
                };
                _loaded[guid] = info;
                _loadedOrder.Add(info);
                _slots[initialSlot] = guid;
                _pendingInitialization.Add(info);
                SceneEvents.RaiseSceneLoaded(info);
            }

            // An empty operation reconciles the state, which loads whatever the adopted scene depends on.
            await new SceneOperation().WithInputBlocking(false).PerformAsync();
        }

        /// <summary>
        /// Editor play mode: loads the scene that was open in the editor, with its dependencies first, then removes the bootstrap scene.
        /// </summary>
        internal async Awaitable RunEditorPlayTargetAsync(string guid)
        {
            if (_catalog == null) return;

            Scene bootstrapScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            SlotId slot = _catalog.Data.InitialSlot;
            if (slot.IsNone)
            {
                Debug.LogError("Scenes has no initial slot. Set one in Tools > Scene System > Scene Config.");
                return;
            }

            var operation = new SceneOperation()
                .Load(slot, SceneRef.FromGuid(guid), setActive: true)
                .WithInputBlocking(false);
            await operation.PerformAsync();

            if (bootstrapScene.IsValid() && bootstrapScene.isLoaded)
                await _loader.UnloadAsync(bootstrapScene, bootstrapScene.path);
        }

        #endregion
    }
}
