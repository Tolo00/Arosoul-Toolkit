using System;
using System.Text.RegularExpressions;

namespace ProjectWizard
{
    public enum PackageSource
    {
        /// <summary>Installed through the Package Manager (git URL or registry name).</summary>
        Upm,

        /// <summary>Unity Asset Store asset you own. Imported from the local download cache.</summary>
        AssetStore,
    }

    /// <summary>One entry in the package list. Add or edit these in <see cref="WizardConfig"/>.</summary>
    public sealed class PackageDefinition
    {
        /// <summary>Unique key. Used by <see cref="Requires"/>.</summary>
        public string Id;

        public string Name;
        public string Description;

        /// <summary>Group heading in the list. Groups appear in order of first use.</summary>
        public string Category;

        public PackageSource Source = PackageSource.Upm;
        public bool SelectedByDefault = false;

        /// <summary>Helper that is only pulled in through <see cref="Requires"/> and not shown in the list.</summary>
        public bool Hidden;

        // ── UPM ───────────────────────────────────────────────────────────

        /// <summary>Git URL or registry name, exactly what you would paste in "Add package from git URL".</summary>
        public string UpmId;

        /// <summary>Package name in manifest.json. Only needed for packages the wizard removes again.</summary>
        public string PackageName;

        /// <summary>
        /// NuGet package ids that must be installed (through NuGetForUnity) BEFORE <see cref="UpmId"/> is added.
        /// The id must also be the assembly name (true for R3, ZLinq, ...).
        /// </summary>
        public string[] NuGetPackages = Array.Empty<string>();

        /// <summary>Ids of other definitions that are installed automatically when this one is selected.</summary>
        public string[] Requires = Array.Empty<string>();

        // ── Asset Store ───────────────────────────────────────────────────

        /// <summary>Store page URL. The product id is read from the number at the end of it.</summary>
        public string AssetStoreUrl;

        /// <summary>
        /// Text the downloaded .unitypackage file name must contain (case/punctuation ignored).
        /// Defaults to <see cref="Name"/>. Change it if the wizard can't find a downloaded asset.
        /// </summary>
        public string CacheMatch;

        /// <summary>
        /// Project folder (e.g. "Assets/Plugins" or "Assets/User") the imported asset is moved into.
        /// Leave empty to keep it wherever the asset puts itself.
        /// </summary>
        public string InstallFolder;

        public long AssetStoreProductId
        {
            get
            {
                if (string.IsNullOrEmpty(AssetStoreUrl)) return 0;
                var m = Regex.Match(AssetStoreUrl, @"(\d+)/?(?:[?#].*)?$");
                return m.Success && long.TryParse(m.Groups[1].Value, out var id) ? id : 0;
            }
        }

        public bool NeedsNuGet => NuGetPackages != null && NuGetPackages.Length > 0;

        public string SourceTag =>
            Source == PackageSource.AssetStore ? "Asset Store" :
            NeedsNuGet ? "NuGet + UPM" : "UPM";
    }

    /// <summary>A file written once when folders are created (never overwritten).</summary>
    public sealed class FileTemplate
    {
        public string Path;
        public string Content;
    }
}
