using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace ProjectWizard
{
    /// <summary>
    /// Installs UPM packages with ONE AddAndRemove request instead of one Add per package.
    /// One manifest change means one resolve and one recompile, instead of a reload after every package.
    ///
    /// If the batch fails (one bad URL fails them all), it retries package by package so a single
    /// broken entry can't block the others.
    /// </summary>
    internal sealed class UpmStageRunner : IStageRunner
    {
        private const int MaxReloadRetries = 5;

        private AddAndRemoveRequest _request;
        private List<string> _requested;

        public bool Tick(RunState run, Stage stage)
        {
            if (_request == null)
                return StartNext(stage);

            if (!_request.IsCompleted)
                return false;

            stage.inFlight = false;

            if (_request.Status == StatusCode.Success)
            {
                foreach (var id in _requested)
                {
                    stage.pending.Remove(id);
                    WizardLog.Success($"  ✓ {PackageLookup.NameForUpm(id)}");
                }
            }
            else
            {
                var error = _request.Error?.message ?? "unknown error";
                if (!stage.single && _requested.Count > 1)
                {
                    stage.single = true;
                    WizardLog.Warn($"Batch install failed ({FirstLine(error)}). Retrying one by one…");
                }
                else
                {
                    var id = _requested[0];
                    stage.pending.Remove(id);
                    stage.failed.Add(id);
                    WizardLog.Error($"  ✗ {PackageLookup.NameForUpm(id)}: {FirstLine(error)}");
                }
            }

            _request = null;
            return stage.pending.Count == 0;
        }

        private bool StartNext(Stage stage)
        {
            // A request was running but we have no handle: a domain reload interrupted us.
            // The manifest is the source of truth for what actually got added.
            if (stage.inFlight)
            {
                stage.inFlight = false;
                stage.reloads++;

                var manifest = PackageLookup.ManifestText();
                stage.pending.RemoveAll(id => PackageLookup.IsInManifest(id, manifest));

                if (stage.reloads > MaxReloadRetries && stage.pending.Count > 0)
                {
                    stage.failed.AddRange(stage.pending);
                    WizardLog.Error($"Gave up on {stage.pending.Count} package(s) after repeated interruptions.");
                    stage.pending.Clear();
                }
            }

            if (stage.pending.Count == 0)
                return true;

            _requested = stage.single
                ? new List<string> { stage.pending[0] }
                : new List<string>(stage.pending);

            WizardLog.Info(_requested.Count == 1
                ? $"Installing {PackageLookup.NameForUpm(_requested[0])}…"
                : $"Installing {_requested.Count} packages…");

            stage.inFlight = true;
            _request = Client.AddAndRemove(_requested.ToArray());
            return false;
        }

        public void Reset()
        {
            _request = null;
            _requested = null;
        }

        private static string FirstLine(string text)
        {
            var i = text.IndexOf('\n');
            return i < 0 ? text : text.Substring(0, i).Trim();
        }
    }
}
