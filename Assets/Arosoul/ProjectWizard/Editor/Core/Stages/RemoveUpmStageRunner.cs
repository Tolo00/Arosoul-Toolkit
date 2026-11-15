using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace ProjectWizard
{
    /// <summary>
    /// Removes UPM packages again (used to take NuGetForUnity out once the NuGet DLLs are installed).
    /// The DLLs stay in the project, only the helper package goes.
    /// </summary>
    internal sealed class RemoveUpmStageRunner : IStageRunner
    {
        private const int MaxReloadRetries = 5;
        private AddAndRemoveRequest _request;

        public bool Tick(RunState run, Stage stage)
        {
            if (_request == null)
            {
                // After a reload mid-removal the manifest tells what is already gone.
                if (stage.inFlight) { stage.inFlight = false; stage.reloads++; }

                var manifest = PackageLookup.ManifestText();
                stage.pending.RemoveAll(id => !PackageLookup.IsInManifest(id, manifest));

                if (stage.pending.Count == 0 || stage.reloads > MaxReloadRetries)
                {
                    if (stage.pending.Count == 0) WizardLog.Success("  ✓ Removed NuGetForUnity (NuGet DLLs stay in the project).");
                    else WizardLog.Warn("Couldn't remove NuGetForUnity. Remove it in the Package Manager if you want it gone.");
                    stage.pending.Clear();
                    return true;
                }

                var names = stage.pending
                    .Select(id => PackageLookup.ManifestNameFor(id) ?? PackageLookup.ById(WizardConfig.NuGetHelperId)?.PackageName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToArray();

                if (names.Length == 0)
                {
                    WizardLog.Warn("Couldn't work out NuGetForUnity's package name. Remove it in the Package Manager.");
                    stage.pending.Clear();
                    return true;
                }

                WizardLog.Info("Removing NuGetForUnity…");
                stage.inFlight = true;
                _request = Client.AddAndRemove(null, names);
                return false;
            }

            if (!_request.IsCompleted) return false;

            stage.inFlight = false;
            if (_request.Status == StatusCode.Success)
            {
                WizardLog.Success("  ✓ Removed NuGetForUnity (NuGet DLLs stay in the project).");
            }
            else
            {
                WizardLog.Warn($"Couldn't remove NuGetForUnity: {_request.Error?.message}");
                stage.failed.AddRange(stage.pending);
            }

            stage.pending.Clear();
            _request = null;
            return true;
        }

        public void Reset() => _request = null;
    }
}
