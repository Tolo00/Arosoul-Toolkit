using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace ProjectWizard
{
    /// <summary>Finds downloaded Asset Store packages in Unity's local download cache.</summary>
    internal static class AssetStoreCache
    {
        /// <summary>Newest .unitypackage whose file name contains the definition's match text, or null.</summary>
        public static string FindPackage(PackageDefinition def)
        {
            var root = WizardPaths.AssetStoreCache;
            if (!Directory.Exists(root)) return null;

            var needle = Normalize(string.IsNullOrEmpty(def.CacheMatch) ? def.Name : def.CacheMatch);
            if (needle.Length == 0) return null;

            try
            {
                return Directory
                    .EnumerateFiles(root, "*.unitypackage", SearchOption.AllDirectories)
                    .Where(f => Normalize(Path.GetFileNameWithoutExtension(f)).Contains(needle))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null; // folder busy or unreadable, try again next poll
            }
        }

        private static string Normalize(string s) =>
            new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>
    /// Opens the Package Manager on an Asset Store product. There is no public API for this, so it
    /// tries several routes, from most to least precise, and stops at the first that doesn't throw.
    /// </summary>
    internal static class AssetStorePackageManager
    {
        public static void Open(PackageDefinition def)
        {
            var id = def.AssetStoreProductId;

            if (id > 0)
            {
                // 1. Internal: select the product in the "My Assets" tab, filtered by name.
                if (TryInternalSelect(id, def.Name)) return;

                // 2. Public: open Package Manager and select by id.
                try { Window.Open(id.ToString()); return; }
                catch (Exception e) { Debug.Log($"[Project Wizard] Window.Open failed: {e.Message}"); }

                // 3. The protocol the store's "Open in Unity" button uses.
                try { Application.OpenURL($"com.unity3d.kharma:content/{id}"); return; }
                catch (Exception e) { Debug.Log($"[Project Wizard] kharma link failed: {e.Message}"); }
            }

            // 4. Last resort: the store page in the browser.
            if (!string.IsNullOrEmpty(def.AssetStoreUrl))
                Application.OpenURL(def.AssetStoreUrl);
        }

        private static bool TryInternalSelect(long id, string name)
        {
            try
            {
                var type = typeof(Window).Assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
                var method = type?.GetMethod("SelectPackageAndFilterStatic",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) return false;

                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                var stringsSeen = 0;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i];
                    var underlying = Nullable.GetUnderlyingType(p.ParameterType) ?? p.ParameterType;

                    if (p.ParameterType == typeof(string))
                        args[i] = stringsSeen++ == 0 ? id.ToString() : name;      // packageToSelect, searchText
                    else if (underlying.IsEnum)
                        args[i] = Enum.TryParse(underlying, "AssetStore", out var tab) ? tab : null;
                    else if (underlying == typeof(bool))
                        args[i] = false;                                           // refresh
                    else
                        args[i] = p.HasDefaultValue ? p.DefaultValue : null;
                }

                method.Invoke(null, args);
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"[Project Wizard] Internal Package Manager call failed: {e.Message}");
                return false;
            }
        }
    }
}
