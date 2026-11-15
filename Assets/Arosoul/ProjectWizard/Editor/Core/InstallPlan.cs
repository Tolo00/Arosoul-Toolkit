using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectWizard
{
    public enum StageKind { Upm = 0, NuGet = 1, AssetStore = 2, RemoveUpm = 3 }

    /// <summary>One step of an install run. Everything in here is saved, so it survives domain reloads.</summary>
    [Serializable]
    public class Stage
    {
        public StageKind kind;

        /// <summary>UPM ids, NuGet ids or definition ids that are still to do.</summary>
        public List<string> pending = new List<string>();

        public List<string> failed = new List<string>();

        public int phase;
        public bool single;        // UPM: install one at a time after a batch failed
        public bool inFlight;      // UPM: a request was running (lost if a reload happens)
        public int reloads;        // UPM: how often a reload interrupted a request
        public bool skipRequested; // Asset Store: user pressed Skip
        public bool restartedRestore;
        public string current;     // Asset Store: definition id being processed
        public bool snapshotTaken; // Asset Store: top-level folders were recorded before the import
        public List<string> snapshot = new List<string>();
        public long phaseStartedTicks;
        public List<string> versions = new List<string>(); // NuGet: "id=version" (packages + their dependencies)
        public List<string> toScan = new List<string>();   // NuGet: entries whose dependencies are still to look up
    }

    [Serializable]
    public class RunState
    {
        public bool active;
        public int index;
        public long notBeforeTicks; // short pause between stages so compiles can start
        public List<Stage> stages = new List<Stage>();

        public Stage Current => active && index >= 0 && index < stages.Count ? stages[index] : null;
    }

    /// <summary>Lookups and "is it already installed" checks.</summary>
    public static class PackageLookup
    {
        public static PackageDefinition ById(string id) =>
            WizardConfig.Packages.FirstOrDefault(p => p.Id == id);

        /// <summary>The key a package has in manifest.json, found through its URL/id. Null if not found.</summary>
        public static string ManifestNameFor(string upmId)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                ManifestText(), "\"([^\"]+)\"\\s*:\\s*\"" + System.Text.RegularExpressions.Regex.Escape(upmId) + "\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static string NameForUpm(string upmId) =>
            WizardConfig.Packages.FirstOrDefault(p => p.UpmId == upmId)?.Name ?? upmId;

        public static string ManifestText()
        {
            try { return File.ReadAllText(WizardPaths.ManifestPath); }
            catch { return string.Empty; }
        }

        public static bool IsInManifest(string upmId, string manifestText = null)
        {
            manifestText ??= ManifestText();
            if (string.IsNullOrEmpty(upmId)) return false;

            // Git URLs are stored verbatim as the value. Registry ids ("com.x@1.2.3") are stored as a key.
            if (upmId.Contains("://") || upmId.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                return manifestText.Contains(upmId);

            var name = upmId.Split('@')[0];
            return manifestText.Contains("\"" + name + "\"");
        }

        public static bool IsAssemblyLoaded(string assemblyName) =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

        public static bool IsInstalled(PackageDefinition def)
        {
            if (def.Source != PackageSource.Upm) return false; // Asset Store imports can't be detected reliably
            if (!IsInManifest(def.UpmId)) return false;
            return def.NuGetPackages.All(IsAssemblyLoaded);
        }
    }

    internal static class InstallPlan
    {
        /// <summary>
        /// Turns the selection into ordered stages:
        /// UPM packages → NuGet packages → UPM packages that need NuGet → Asset Store imports.
        /// The order matters: an adapter like R3.Unity breaks compilation if R3.dll isn't there yet.
        /// </summary>
        public static RunState Build(IEnumerable<PackageDefinition> selected)
        {
            var all = ExpandRequirements(selected);
            var manifest = PackageLookup.ManifestText();

            var plainUpm = all
                .Where(p => p.Source == PackageSource.Upm && !p.NeedsNuGet && !PackageLookup.IsInManifest(p.UpmId, manifest))
                .Select(p => p.UpmId).Distinct().ToList();

            var nuget = all
                .Where(p => p.Source == PackageSource.Upm)
                .SelectMany(p => p.NuGetPackages)
                .Where(id => !PackageLookup.IsAssemblyLoaded(id))
                .Distinct().ToList();

            var nugetUpm = all
                .Where(p => p.Source == PackageSource.Upm && p.NeedsNuGet && !PackageLookup.IsInManifest(p.UpmId, manifest))
                .Select(p => p.UpmId).Distinct().ToList();

            var assets = all
                .Where(p => p.Source == PackageSource.AssetStore)
                .Select(p => p.Id).Distinct().ToList();

            // The NuGet helper is only removed again if this run is the one that installed it.
            var helper = PackageLookup.ById(WizardConfig.NuGetHelperId);
            var removeHelper = helper != null
                               && all.Contains(helper)
                               && !PackageLookup.IsInManifest(helper.UpmId, manifest);

            var run = new RunState();
            AddStage(run, StageKind.Upm, plainUpm);
            AddStage(run, StageKind.NuGet, nuget);
            AddStage(run, StageKind.Upm, nugetUpm);
            AddStage(run, StageKind.AssetStore, assets);
            if (removeHelper)
                AddStage(run, StageKind.RemoveUpm, new List<string> { helper.UpmId });
            return run;
        }

        private static void AddStage(RunState run, StageKind kind, List<string> pending)
        {
            if (pending.Count == 0) return;
            run.stages.Add(new Stage { kind = kind, pending = pending });
        }

        private static List<PackageDefinition> ExpandRequirements(IEnumerable<PackageDefinition> selected)
        {
            var result = new List<PackageDefinition>();
            var queue = new Queue<PackageDefinition>(selected);

            while (queue.Count > 0)
            {
                var def = queue.Dequeue();
                if (result.Contains(def)) continue;
                result.Add(def);

                foreach (var reqId in def.Requires ?? Array.Empty<string>())
                {
                    var req = PackageLookup.ById(reqId);
                    if (req == null)
                    {
                        WizardLog.Warn($"{def.Name} requires unknown package id '{reqId}' (check WizardConfig).");
                        continue;
                    }

                    if (!result.Contains(req) && !queue.Contains(req))
                        WizardLog.Info($"Also adding {req.Name} (required by {def.Name}).");
                    queue.Enqueue(req);
                }
            }

            return result;
        }
    }
}
