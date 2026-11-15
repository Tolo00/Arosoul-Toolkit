using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectWizard
{
    /// <summary>
    /// Installs NuGet packages through NuGetForUnity without any clicking:
    ///   0. wait until NuGetForUnity has compiled, look up the newest stable version of each package
    ///   1. add the packages to Assets/packages.config
    ///   2. run NuGetForUnity's "Restore Packages" menu item
    ///   3. wait until the assemblies are loaded
    /// </summary>
    internal sealed class NuGetStageRunner : IStageRunner
    {
        private const string RestoreMenuItem = "NuGet/Restore Packages";

        private static readonly TimeSpan WaitForNuGetTimeout = TimeSpan.FromSeconds(120);
        private static readonly TimeSpan RestoreMenuTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan AssembliesTimeout = TimeSpan.FromSeconds(240);
        private static readonly TimeSpan RestoreRetryAfter = TimeSpan.FromSeconds(45);

        private UnityWebRequest _web;
        private string _webId;

        [Serializable] private class VersionList { public string[] versions; }

        public bool Tick(RunState run, Stage stage)
        {
            switch (stage.phase)
            {
                case 0: return ResolveVersions(stage);
                case 10: return ScanDependencies(stage);
                case 1: return WriteConfig(stage);
                case 5: return WaitForReload(stage);
                case 2: return RunRestore(stage);
                default: return WaitForAssemblies(stage);
            }
        }

        // ── phase 0 ──────────────────────────────────────────────────────

        private bool ResolveVersions(Stage stage)
        {
            if (stage.phaseStartedTicks == 0) stage.phaseStartedTicks = DateTime.UtcNow.Ticks;

            if (!PackageLookup.IsAssemblyLoaded("NuGetForUnity") &&
                !AnyAssemblyContains("NuGetForUnity"))
            {
                if (Elapsed(stage) > WaitForNuGetTimeout)
                    return Fail(stage, "NuGetForUnity never finished loading. Check the Console for compile errors, then run again.");
                return false;
            }

            // finished looking up everything?
            var next = stage.pending.FirstOrDefault(id => !HasVersion(stage, id));
            if (next == null)
            {
                stage.toScan = new List<string>(stage.versions);
                SetPhase(stage, 10);
                return false;
            }

            if (_web == null)
            {
                _webId = next;
                _web = UnityWebRequest.Get($"https://api.nuget.org/v3-flatcontainer/{next.ToLowerInvariant()}/index.json");
                _web.SendWebRequest();
                return false;
            }

            if (!_web.isDone) return false;

            string version = null;
            if (_web.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var list = JsonUtility.FromJson<VersionList>(_web.downloadHandler.text);
                    version = list?.versions?.LastOrDefault(v => !v.Contains("-")); // newest stable
                }
                catch { /* handled below */ }
            }

            if (version == null)
            {
                WizardLog.Error($"  ✗ NuGet: couldn't find a version of {_webId} ({_web.error ?? "no stable version"}).");
                stage.failed.Add(_webId);
                stage.pending.Remove(_webId);
                if (stage.pending.Count == 0) { Reset(); return true; }
            }
            else
            {
                stage.versions.Add($"{_webId}={version}");
                WizardLog.Info($"  NuGet {_webId} {version}");
            }

            _web.Dispose();
            _web = null;
            return false;
        }

        // ── phase 10: dependencies ───────────────────────────────────────

        // Restore only installs what is listed in packages.config, it does not pull in dependencies.
        // So the whole dependency tree (R3 needs Microsoft.Bcl.TimeProvider, System.Threading.Channels, ...)
        // is looked up in each package's .nuspec and listed too.
        private bool ScanDependencies(Stage stage)
        {
            if (stage.toScan.Count == 0)
            {
                SetPhase(stage, 1);
                return false;
            }

            var entry = stage.toScan[0];
            var parts = entry.Split('=');

            if (_web == null)
            {
                var id = parts[0].ToLowerInvariant();
                var ver = parts[1].ToLowerInvariant();
                _web = UnityWebRequest.Get($"https://api.nuget.org/v3-flatcontainer/{id}/{ver}/{id}.nuspec");
                _web.SendWebRequest();
                return false;
            }

            if (!_web.isDone) return false;

            stage.toScan.RemoveAt(0);

            if (_web.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    foreach (var (depId, depVersion) in ReadDependencies(_web.downloadHandler.text))
                    {
                        if (HasVersion(stage, depId) || PackageLookup.IsAssemblyLoaded(depId)) continue;

                        stage.versions.Add($"{depId}={depVersion}");
                        stage.toScan.Add($"{depId}={depVersion}");
                        WizardLog.Info($"  NuGet {depId} {depVersion} (needed by {parts[0]})");
                    }
                }
                catch (Exception e)
                {
                    WizardLog.Warn($"  Couldn't read dependencies of {parts[0]}: {e.Message}");
                }
            }
            else
            {
                WizardLog.Warn($"  Couldn't look up dependencies of {parts[0]}: {_web.error}");
            }

            _web.Dispose();
            _web = null;
            return false;
        }

        private static List<(string id, string version)> ReadDependencies(string nuspecText)
        {
            var result = new List<(string, string)>();
            var doc = XDocument.Parse(nuspecText.TrimStart('\uFEFF'));

            var deps = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dependencies");
            if (deps == null) return result;

            // Unity uses netstandard2.1 (or 2.0). Take the best matching group.
            var groups = deps.Elements().Where(e => e.Name.LocalName == "group").ToList();
            IEnumerable<XElement> chosen;

            if (groups.Count == 0)
            {
                chosen = deps.Elements().Where(e => e.Name.LocalName == "dependency");
            }
            else
            {
                var group =
                    FindGroup(groups, ".netstandard2.1") ??
                    FindGroup(groups, ".netstandard2.0") ??
                    groups.FirstOrDefault(g => g.Attribute("targetFramework") == null);

                if (group == null) return result;
                chosen = group.Elements().Where(e => e.Name.LocalName == "dependency");
            }

            foreach (var d in chosen)
            {
                var id = (string)d.Attribute("id");
                var range = (string)d.Attribute("version") ?? "";
                // "[1.2.3, )" / "(, 2.0)" / "1.2.3" → the lower bound
                var version = range.Trim('[', ']', '(', ')', ' ').Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                    result.Add((id, version));
            }

            return result;
        }

        private static XElement FindGroup(List<XElement> groups, string framework) =>
            groups.FirstOrDefault(g =>
                string.Equals((string)g.Attribute("targetFramework"), framework, StringComparison.OrdinalIgnoreCase));

        // ── phase 1 ──────────────────────────────────────────────────────

        private bool WriteConfig(Stage stage)
        {
            try
            {
                var path = WizardPaths.PackagesConfigPath;
                XDocument doc;
                XElement root;

                if (File.Exists(path) && (doc = XDocument.Load(path)).Root != null)
                {
                    root = doc.Root;
                }
                else
                {
                    root = new XElement("packages");
                    doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
                }

                foreach (var entry in stage.versions)
                {
                    var parts = entry.Split('=');
                    var id = parts[0];
                    var version = parts[1];

                    var existing = root.Elements("package").FirstOrDefault(e =>
                        string.Equals((string)e.Attribute("id"), id, StringComparison.OrdinalIgnoreCase));

                    if (existing != null) continue; // never touch what's already there

                    var element = new XElement("package");
                    element.SetAttributeValue("id", id);
                    element.SetAttributeValue("version", version);
                    element.SetAttributeValue("manuallyInstalled", "true");
                    root.Add(element);
                }

                doc.Save(path);

                // Put the DLLs in the configured folder (Assets/Plugins/NuGet) instead of Assets/Packages.
                NuGetConfigFile.EnsureRepositoryPath();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                return Fail(stage, $"Couldn't write packages.config: {e.Message}");
            }

            // NuGetForUnity reads NuGet.config once per domain, so reload scripts to make it see the change.
            SetPhase(stage, 5);
            EditorUtility.RequestScriptReload();
            return false;
        }

        private bool WaitForReload(Stage stage)
        {
            if (Elapsed(stage) < TimeSpan.FromSeconds(3)) return false;
            SetPhase(stage, 2);
            return false;
        }

        // ── phase 2 ──────────────────────────────────────────────────────

        private bool RunRestore(Stage stage)
        {
            if (EditorApplication.ExecuteMenuItem(RestoreMenuItem))
            {
                WizardLog.Info("  Restoring NuGet packages…");
                SetPhase(stage, 3);
                return false;
            }

            if (Elapsed(stage) > RestoreMenuTimeout)
                return Fail(stage, $"Menu item '{RestoreMenuItem}' not found. Run NuGet > Restore Packages manually.");

            return false; // menu may not be registered yet, try again next tick
        }

        // ── phase 3 ──────────────────────────────────────────────────────

        private bool WaitForAssemblies(Stage stage)
        {
            var loaded = stage.pending.Where(PackageLookup.IsAssemblyLoaded).ToList();
            foreach (var id in loaded)
            {
                stage.pending.Remove(id);
                WizardLog.Success($"  ✓ NuGet {id}");
            }

            if (stage.pending.Count == 0) return true;

            var elapsed = Elapsed(stage);

            // Sometimes the first restore runs before the config is picked up. Once more is cheap.
            if (!stage.restartedRestore && elapsed > RestoreRetryAfter)
            {
                stage.restartedRestore = true;
                EditorApplication.ExecuteMenuItem(RestoreMenuItem);
            }

            if (elapsed > AssembliesTimeout)
                return Fail(stage, $"NuGet packages didn't load in time: {string.Join(", ", stage.pending)}. Check NuGet > Manage NuGet Packages.");

            return false;
        }

        // ── helpers ──────────────────────────────────────────────────────

        private static bool HasVersion(Stage stage, string id) =>
            stage.versions.Any(v => v.StartsWith(id + "="));

        private static bool AnyAssemblyContains(string text) =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                a.GetName().Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

        private static void SetPhase(Stage stage, int phase)
        {
            stage.phase = phase;
            stage.phaseStartedTicks = DateTime.UtcNow.Ticks;
        }

        private static TimeSpan Elapsed(Stage stage) =>
            stage.phaseStartedTicks == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks(DateTime.UtcNow.Ticks - stage.phaseStartedTicks);

        private static bool Fail(Stage stage, string message)
        {
            WizardLog.Error(message);
            stage.failed.AddRange(stage.pending);
            stage.pending.Clear();
            return true;
        }

        public void Reset()
        {
            _web?.Dispose();
            _web = null;
            _webId = null;
        }
    }
}
