using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace ProjectWizard
{
    /// <summary>
    /// Unity has no public API to download Asset Store purchases, so this does the next best thing:
    ///   - already downloaded → imported silently from the local cache
    ///   - not downloaded     → opens the Package Manager on that asset, you press Download once,
    ///                          the wizard notices the finished file and imports it by itself
    /// </summary>
    internal sealed class AssetStoreStageRunner : IStageRunner
    {
        private const int PhaseLookup = 0;
        private const int PhaseImporting = 1;
        private const int PhaseWaitingForDownload = 2;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        // 0 = nothing yet, 1 = ok, 2 = failed
        private static int _importResult;
        private bool _importStartedThisDomain;
        private DateTime _nextPoll;
        private string _candidate;
        private long _candidateSize = -1;

        public AssetStoreStageRunner()
        {
            AssetDatabase.importPackageCompleted += _ => _importResult = 1;
            AssetDatabase.importPackageFailed += (_, _) => _importResult = 2;
            AssetDatabase.importPackageCancelled += _ => _importResult = 2;
        }

        public bool Tick(RunState run, Stage stage)
        {
            if (string.IsNullOrEmpty(stage.current))
            {
                if (stage.pending.Count == 0) return true;

                stage.current = stage.pending[0];
                stage.phase = PhaseLookup;
                stage.skipRequested = false;
                ResetTransient();
            }

            var def = PackageLookup.ById(stage.current);
            if (def == null)
            {
                Finish(stage, null, false);
                return false;
            }

            if (stage.skipRequested)
            {
                WizardLog.Warn($"  ↷ Skipped {def.Name}");
                Finish(stage, def, false);
                return false;
            }

            switch (stage.phase)
            {
                case PhaseLookup:         LookupAndImport(stage, def); break;
                case PhaseImporting:      WaitForImport(stage, def); break;
                default:                  WaitForDownload(stage, def); break;
            }

            return false;
        }

        private void LookupAndImport(Stage stage, PackageDefinition def)
        {
            var file = AssetStoreCache.FindPackage(def);
            if (file != null)
            {
                WizardLog.Info($"Importing {def.Name} from the download cache…");
                _importResult = 0;
                _importStartedThisDomain = true;
                stage.snapshot = TopLevelFolders();
                stage.snapshotTaken = true;
                stage.phase = PhaseImporting;
                AssetDatabase.ImportPackage(file, false); // false = no import dialog
                return;
            }

            WizardLog.Info($"{def.Name} isn't downloaded yet. Opening the Package Manager: press Download, " +
                           "it is imported automatically when it finishes. (Skip if you don't want it.)");
            AssetStorePackageManager.Open(def);
            stage.phase = PhaseWaitingForDownload;
            _nextPoll = DateTime.UtcNow + PollInterval;
        }

        private void WaitForImport(Stage stage, PackageDefinition def)
        {
            // A domain reload during the import wipes _importStartedThisDomain. By the time we are
            // running again the import has finished (Unity reloads after it), so count it as done.
            if (!_importStartedThisDomain)
            {
                Relocate(stage, def);
                WizardLog.Success($"  ✓ {def.Name}");
                Finish(stage, def, true);
                return;
            }

            if (_importResult == 0) return;

            if (_importResult == 1)
            {
                Relocate(stage, def);
                WizardLog.Success($"  ✓ {def.Name}");
                Finish(stage, def, true);
            }
            else
            {
                WizardLog.Error($"  ✗ {def.Name}: import failed or was cancelled.");
                Finish(stage, def, false);
            }
        }

        private void WaitForDownload(Stage stage, PackageDefinition def)
        {
            if (DateTime.UtcNow < _nextPoll) return;
            _nextPoll = DateTime.UtcNow + PollInterval;

            var file = AssetStoreCache.FindPackage(def);
            if (file == null)
            {
                _candidate = null;
                _candidateSize = -1;
                return;
            }

            // Only import once the file stopped growing between two polls.
            long size;
            try { size = new FileInfo(file).Length; }
            catch (Exception) { return; }

            if (file == _candidate && size == _candidateSize && size > 0)
            {
                stage.phase = PhaseLookup; // next tick finds it and imports
                return;
            }

            _candidate = file;
            _candidateSize = size;
        }

        // ── moving imported assets into def.InstallFolder ────────────────

        // Folders Unity or other tools treat specially. Moving them would break things.
        private static readonly HashSet<string> KeepInPlace = new HashSet<string>
        {
            "Plugins", "User", "Resources", "Gizmos", "StreamingAssets", "Editor Default Resources", "_Project",
        };

        private static List<string> TopLevelFolders() =>
            AssetDatabase.GetSubFolders("Assets").Select(f => f.Substring("Assets/".Length)).ToList();

        /// <summary>
        /// Most assets create their own folder directly under Assets. Any top-level folder that appeared
        /// during the import is moved into the configured folder (GUIDs stay intact).
        /// </summary>
        private static void Relocate(Stage stage, PackageDefinition def)
        {
            if (string.IsNullOrEmpty(def.InstallFolder) || !stage.snapshotTaken) return;

            var target = def.InstallFolder.Replace('\\', '/').TrimEnd('/');
            if (!target.StartsWith("Assets/")) return;

            EnsureFolder(target);

            foreach (var folder in AssetDatabase.GetSubFolders("Assets"))
            {
                var name = folder.Substring("Assets/".Length);
                if (stage.snapshot.Contains(name) || KeepInPlace.Contains(name)) continue;

                var error = AssetDatabase.MoveAsset(folder, target + "/" + name);
                if (string.IsNullOrEmpty(error))
                    WizardLog.Info($"  moved {folder} → {target}/{name}");
                else
                    WizardLog.Warn($"  couldn't move {folder} to {target}: {error}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private void Finish(Stage stage, PackageDefinition def, bool success)
        {
            var id = stage.current;
            stage.pending.Remove(id);
            if (!success && def != null && !stage.skipRequested) stage.failed.Add(id);

            stage.current = null;
            stage.skipRequested = false;
            stage.snapshotTaken = false;
            ResetTransient();
        }

        private void ResetTransient()
        {
            _importStartedThisDomain = false;
            _importResult = 0;
            _candidate = null;
            _candidateSize = -1;
        }

        public void Reset() => ResetTransient();
    }
}
