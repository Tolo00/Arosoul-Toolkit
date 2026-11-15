using System;
using UnityEditor;
using UnityEngine;

namespace ProjectWizard
{
    /// <summary>
    /// Runs the install stages one after another.
    ///
    /// Installing a package recompiles scripts, which wipes all static state (a domain reload).
    /// The old script kept its queue in memory, so the queue vanished after the first package.
    /// Here the whole run lives in SessionState and is picked up again by the static constructor
    /// after every reload.
    /// </summary>
    [InitializeOnLoad]
    public static class InstallOrchestrator
    {
        private const string StateKey = "ProjectWizard.Run";
        private const double TickInterval = 0.4;
        private static readonly TimeSpan StageSettleDelay = TimeSpan.FromSeconds(3);

        private static readonly IStageRunner[] Runners =
        {
            new UpmStageRunner(),        // StageKind.Upm
            new NuGetStageRunner(),      // StageKind.NuGet
            new AssetStoreStageRunner(), // StageKind.AssetStore
            new RemoveUpmStageRunner(),  // StageKind.RemoveUpm
        };

        private static RunState _state;
        private static double _nextTick;

        public static event Action Changed;

        public static bool IsRunning => _state != null && _state.active;
        public static RunState State => _state;

        static InstallOrchestrator()
        {
            _state = Load();
            if (_state.active)
                EditorApplication.delayCall += Attach;
        }

        // ── public API ───────────────────────────────────────────────────

        internal static void Start(RunState run)
        {
            if (IsRunning) return;

            run.active = true;
            run.index = 0;
            _state = run;
            Save();
            Attach();
            Changed?.Invoke();
        }

        public static void Cancel()
        {
            if (!IsRunning) return;
            Finish("Cancelled.", LogLevel.Warning);
        }

        /// <summary>Gives up on the current Asset Store item and moves on.</summary>
        public static void SkipCurrent()
        {
            var stage = _state?.Current;
            if (stage == null) return;

            if (stage.kind == StageKind.AssetStore)
            {
                stage.skipRequested = true;
                Save();
            }
        }

        public static string Describe()
        {
            var run = _state;
            if (run == null || !run.active || run.Current == null) return "Idle";

            var stage = run.Current;
            var step = $"Step {run.index + 1}/{run.stages.Count}";
            switch (stage.kind)
            {
                case StageKind.Upm:
                    return $"{step}: installing packages ({stage.pending.Count} left)";
                case StageKind.NuGet:
                    return $"{step}: installing NuGet packages ({stage.pending.Count} left)";
                case StageKind.RemoveUpm:
                    return $"{step}: cleaning up (removing NuGetForUnity)";
                default:
                    var def = PackageLookup.ById(stage.current ?? "");
                    var what = def != null ? def.Name : "Asset Store";
                    return $"{step}: {what} ({stage.pending.Count} left)";
            }
        }

        // ── loop ─────────────────────────────────────────────────────────

        private static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Detach() => EditorApplication.update -= Tick;

        private static void Tick()
        {
            if (!IsRunning) { Detach(); return; }

            if (EditorApplication.timeSinceStartup < _nextTick) return;
            _nextTick = EditorApplication.timeSinceStartup + TickInterval;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (DateTime.UtcNow.Ticks < _state.notBeforeTicks) return;

            var stage = _state.Current;
            if (stage == null) { Finish(null, LogLevel.Success); return; }

            var runner = Runners[(int)stage.kind];
            var done = false;
            try
            {
                done = runner.Tick(_state, stage);
            }
            catch (Exception e)
            {
                WizardLog.Error($"Unexpected error: {e.Message}");
                Debug.LogException(e);
                stage.failed.AddRange(stage.pending);
                stage.pending.Clear();
                done = true;
            }

            if (done)
            {
                runner.Reset();
                _state.index++;
                _state.notBeforeTicks = (DateTime.UtcNow + StageSettleDelay).Ticks;

                if (_state.index >= _state.stages.Count)
                {
                    Save();
                    Finish(null, LogLevel.Success);
                    return;
                }
            }

            Save();
            Changed?.Invoke();
        }

        private static void Finish(string message, LogLevel level)
        {
            var failed = 0;
            foreach (var s in _state.stages) failed += s.failed.Count;

            foreach (var runner in Runners) runner.Reset();

            _state.active = false;
            Save();
            Detach();

            if (message != null)
                WizardLog.Add(level, message);
            else if (failed == 0)
                WizardLog.Success("All done.");
            else
                WizardLog.Warn($"Finished with {failed} problem(s). See the log above.");

            AssetDatabase.Refresh();
            Changed?.Invoke();
        }

        // ── persistence ──────────────────────────────────────────────────

        private static RunState Load()
        {
            var json = SessionState.GetString(StateKey, "");
            if (string.IsNullOrEmpty(json)) return new RunState();
            try { return JsonUtility.FromJson<RunState>(json) ?? new RunState(); }
            catch { return new RunState(); }
        }

        private static void Save() => SessionState.SetString(StateKey, JsonUtility.ToJson(_state));
    }

    internal interface IStageRunner
    {
        /// <summary>Called about twice a second. Return true when the stage is finished.</summary>
        bool Tick(RunState run, Stage stage);

        /// <summary>Forget any non-saved state (running requests etc.).</summary>
        void Reset();
    }
}
