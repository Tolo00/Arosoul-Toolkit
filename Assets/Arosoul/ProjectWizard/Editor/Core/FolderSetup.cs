using System.IO;
using System.Linq;
using UnityEditor;

namespace ProjectWizard
{
    internal static class FolderSetup
    {
        public static void Run()
        {
            WizardLog.Info("Setting up folders…");
            int created = 0, existed = 0;

            foreach (var folder in WizardConfig.Folders)
            {
                if (!IsValid(folder)) continue;

                var full = WizardPaths.ToFullPath(folder);
                if (Directory.Exists(full)) { existed++; continue; }

                Directory.CreateDirectory(full);
                created++;
                WizardLog.Info($"  created {folder}");
            }

            foreach (var file in WizardConfig.Files)
            {
                if (!IsValid(file.Path)) continue;

                var full = WizardPaths.ToFullPath(file.Path);
                if (File.Exists(full)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full, file.Content);
                WizardLog.Info($"  created {file.Path}");
            }

            // Placeholder only in folders that are still completely empty.
            foreach (var folder in WizardConfig.Folders)
            {
                if (!IsValid(folder)) continue;

                var full = WizardPaths.ToFullPath(folder);
                if (Directory.EnumerateFileSystemEntries(full).Any()) continue;

                File.WriteAllText(Path.Combine(full, WizardConfig.PlaceholderFileName), string.Empty);
            }

            AssetDatabase.Refresh();
            WizardLog.Success($"Folders done: {created} created, {existed} already existed.");
        }

        private static bool IsValid(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) &&
                path.Replace('\\', '/').StartsWith("Assets/") &&
                !path.Contains(".."))
                return true;

            WizardLog.Warn($"Ignored invalid path in config: '{path}' (must start with Assets/)");
            return false;
        }
    }
}
