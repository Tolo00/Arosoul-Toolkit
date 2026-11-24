using System;
using System.Collections.Generic;
using System.Text;
using Arosoul.Transitions;
using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Describes a change to the loaded scenes. Build it with the fluent methods, then call <see cref="Perform"/> or <see cref="PerformAsync"/>.
    /// Operations are queued and run one at a time, in the order they were performed.
    /// </summary>
    public sealed class SceneOperation
    {
        /// <summary>
        /// Transitions started by an operation with priority 0 get this priority, so they interrupt ordinary transitions.
        /// </summary>
        public const int DefaultTransitionPriority = 100;

        private readonly List<LoadRequest> _loads = new List<LoadRequest>();
        private readonly List<SlotId> _unloads = new List<SlotId>();

        private TransitionParameters? _transition;
        private bool _unloadUnusedAssets;
        private bool _blockInput = true;
        private object _payload;
        private SceneRef _activeScene;

        public SceneOperationState State { get; private set; } = SceneOperationState.Created;
        public SceneOperationPhase Phase { get; private set; } = SceneOperationPhase.None;
        public float Progress { get; private set; }
        public Exception Error { get; private set; }

        public bool IsFinished => State == SceneOperationState.Completed || State == SceneOperationState.Failed || State == SceneOperationState.Cancelled;

        /// <summary>
        /// Raised when the operation has completed, failed or been cancelled.
        /// </summary>
        public event Action<SceneOperation> Finished;

        internal readonly struct LoadRequest
        {
            public readonly SlotId Slot;
            public readonly SceneRef Scene;
            public readonly bool Reload;

            public LoadRequest(SlotId slot, SceneRef scene, bool reload)
            {
                Slot = slot;
                Scene = scene;
                Reload = reload;
            }
        }

        internal IReadOnlyList<LoadRequest> Loads => _loads;
        internal IReadOnlyList<SlotId> Unloads => _unloads;
        internal TransitionParameters? Transition => _transition;
        internal bool UnloadUnusedAssets => _unloadUnusedAssets;
        internal bool BlockInput => _blockInput;
        internal object Payload => _payload;
        internal SceneRef ActiveScene => _activeScene;

        #region Builder

        /// <summary>
        /// Load a scene into a slot. Whatever is in the slot is unloaded, dependencies are loaded first.
        /// </summary>
        /// <param name="setActive">Make the scene the active scene when it is loaded.</param>
        /// <param name="reload">Unload and load the scene again if it is already loaded.</param>
        public SceneOperation Load(SlotId slot, SceneRef scene, bool setActive = false, bool reload = false)
        {
            if (!EnsureEditable()) return this;
            _loads.Add(new LoadRequest(slot, scene, reload));
            if (setActive) _activeScene = scene;
            return this;
        }

        /// <summary>
        /// Unload the scene in a slot. Dependencies that nothing else needs are unloaded too.
        /// </summary>
        public SceneOperation Unload(SlotId slot)
        {
            if (!EnsureEditable()) return this;
            _unloads.Add(slot);
            return this;
        }

        /// <summary>
        /// Cover the operation with a transition. The transition is held until the operation is done.
        /// Implicitly convertible from a TransitionId: WithTransition(TransitionIds.Fade).
        /// </summary>
        public SceneOperation WithTransition(TransitionParameters parameters)
        {
            if (!EnsureEditable()) return this;
            _transition = parameters;
            return this;
        }

        /// <summary>
        /// Unload unused assets when the scenes have changed.
        /// </summary>
        public SceneOperation WithUnloadUnusedAssets(bool unload = true)
        {
            if (!EnsureEditable()) return this;
            _unloadUnusedAssets = unload;
            return this;
        }

        /// <summary>
        /// Data handed to the initializers of the scenes loaded by this operation.
        /// </summary>
        public SceneOperation WithPayload(object payload)
        {
            if (!EnsureEditable()) return this;
            _payload = payload;
            return this;
        }

        /// <summary>
        /// Block input while the operation runs. Enabled by default. See <see cref="InputBlocker"/>.
        /// </summary>
        public SceneOperation WithInputBlocking(bool block)
        {
            if (!EnsureEditable()) return this;
            _blockInput = block;
            return this;
        }

        private bool EnsureEditable()
        {
            if (State == SceneOperationState.Created) return true;
            Debug.LogWarning("A scene operation can not be changed after it has been performed.");
            return false;
        }

        #endregion

        #region Run

        /// <summary>
        /// Queue the operation.
        /// </summary>
        public void Perform()
        {
            SceneManager.Inst.Enqueue(this);
        }

        /// <summary>
        /// Queue the operation and wait until it has finished. Does not throw if the operation fails, check <see cref="State"/>.
        /// </summary>
        public async Awaitable PerformAsync()
        {
            Perform();
            while (!IsFinished)
                await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// Cancel an operation that is waiting in the queue. A running operation can not be cancelled.
        /// </summary>
        /// <returns>True if the operation was cancelled.</returns>
        public bool Cancel()
        {
            if (State != SceneOperationState.Queued && State != SceneOperationState.Created) return false;
            State = SceneOperationState.Cancelled;
            Finished?.Invoke(this);
            return true;
        }

        #endregion

        #region Internal state

        internal void MarkQueued() => State = SceneOperationState.Queued;
        internal void MarkRunning() => State = SceneOperationState.Running;

        internal void SetPhase(SceneOperationPhase phase)
        {
            Phase = phase;
            SceneEvents.RaisePhaseChanged(this, phase);
        }

        internal void SetProgress(float progress)
        {
            Progress = Mathf.Clamp01(progress);
            SceneEvents.RaiseProgressChanged(this, Progress);
        }

        internal void Complete()
        {
            State = SceneOperationState.Completed;
            Progress = 1f;
            Finished?.Invoke(this);
        }

        internal void Fail(Exception error)
        {
            Error = error;
            State = SceneOperationState.Failed;
            Finished?.Invoke(this);
        }

        #endregion

        /// <summary>
        /// Short human readable description. Used by logs and the debugger.
        /// </summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _loads.Count; i++)
                sb.Append(sb.Length > 0 ? ", " : "").Append("Load ").Append(_loads[i].Slot).Append('=').Append(_loads[i].Scene);
            for (int i = 0; i < _unloads.Count; i++)
                sb.Append(sb.Length > 0 ? ", " : "").Append("Unload ").Append(_unloads[i]);
            return sb.Length == 0 ? "Reconcile" : sb.ToString();
        }
    }
}
