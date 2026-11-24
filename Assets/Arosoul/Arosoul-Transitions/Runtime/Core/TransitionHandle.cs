using System;
using System.Threading;
using UnityEngine;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Handle to a playing transition. Use it to follow the phases, report progress and control when the transition is allowed to exit.
    /// </summary>
    public sealed class TransitionHandle
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _progressDirty;

        public TransitionParameters Parameters { get; }
        public TransitionPhase Phase { get; private set; } = TransitionPhase.Pending;
        public float Progress { get; private set; }
        public string Message { get; private set; } = "";

        public bool IsFinished => Phase == TransitionPhase.Completed || Phase == TransitionPhase.Cancelled;
        public bool IsReleased { get; private set; }

        /// <summary>
        /// Raised every time the phase changes.
        /// </summary>
        public event Action<TransitionPhase> PhaseChanged;

        public TransitionHandle(TransitionParameters parameters)
        {
            Parameters = parameters;
            IsReleased = !parameters.HoldUntilReleased;
        }

        internal CancellationToken Token => _cts.Token;

        /// <summary>
        /// Allow the transition to leave its Hold phase. Only relevant for transitions started with HoldUntilReleased.
        /// </summary>
        public void Release()
        {
            IsReleased = true;
        }

        /// <summary>
        /// Report progress of the work behind the transition. Shown by transitions that support it (loading screens).
        /// </summary>
        public void SetProgress(float progress, string message = null)
        {
            Progress = Mathf.Clamp01(progress);
            if (message != null) Message = message;
            _progressDirty = true;
        }

        /// <summary>
        /// Cancel the transition. Its visuals are removed immediately.
        /// </summary>
        public void Cancel()
        {
            if (IsFinished) return;
            TransitionManager.CancelHandle(this);
        }

        /// <summary>
        /// Wait until the transition has reached the given phase, or has finished.
        /// </summary>
        public async Awaitable WaitForPhaseAsync(TransitionPhase phase)
        {
            while (Phase < phase)
                await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// Wait until the transition is completed or cancelled.
        /// </summary>
        public Awaitable WaitUntilFinishedAsync()
        {
            return WaitForPhaseAsync(TransitionPhase.Completed);
        }

        internal void SetPhase(TransitionPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        internal bool ConsumeProgress(out float progress, out string message)
        {
            progress = Progress;
            message = Message;
            bool dirty = _progressDirty;
            _progressDirty = false;
            return dirty;
        }

        internal void CancelToken()
        {
            if (!_cts.IsCancellationRequested) _cts.Cancel();
        }

        internal void DisposeToken()
        {
            _cts.Dispose();
        }
    }
}
