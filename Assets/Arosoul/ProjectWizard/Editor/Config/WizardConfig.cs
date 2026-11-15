namespace ProjectWizard
{
    /// <summary>
    /// Everything the wizard creates or lists comes from here. No other file needs to change.
    /// </summary>
    public static class WizardConfig
    {
        // ─────────────────────────────────────────────────────────────────
        //  Folders
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Empty folders get this file so Git tracks them. Unity ignores files that start with a dot,
        /// so no .meta files are created for it.
        /// </summary>
        public const string PlaceholderFileName = ".gitkeep";

        /// <summary>Project-relative paths. Parent folders are created automatically.</summary>
        public static readonly string[] Folders =
        {
            "Assets/_Project/Assets/KeyObjects",
            "Assets/_Project/Assets/Graphics",
            "Assets/_Project/Assets/Scripts",
            "Assets/_Project/Data",
            "Assets/_Project/Scenes",
            "Assets/Plugins",
            "Assets/Resources",
        };

        /// <summary>Files created together with the folders. Existing files are never overwritten.</summary>
        public static readonly FileTemplate[] Files =
        {
            new FileTemplate
            {
                Path = "Assets/_Project/Scenes/SceneDatabase.cs",
                Content =
@"using UnityEngine;

/// <summary>
/// Containing static keys and scene strings.
/// </summary>
public static class SceneDatabase
{
    public class Slots
    {
        public const string Main = ""Main"";
    }

    public class Scenes
    {
        public const string MainMenu = ""MainMenu"";
        public const string Game     = ""Game"";
        public const string Credits  = ""Credits"";
    }
}
",
            },
        };

        // ─────────────────────────────────────────────────────────────────
        //  NuGet
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Id (from the list below) of the helper package that is installed for the run and removed afterwards.</summary>
        public const string NuGetHelperId = "nugetforunity";

        /// <summary>Where NuGet DLLs (R3, ZLinq and their dependencies) end up. Must be inside Assets.</summary>
        public const string NuGetInstallFolder = "Assets/Plugins/NuGet";

        // ─────────────────────────────────────────────────────────────────
        //  Asset Store
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Set this if your Asset Store downloads live somewhere non-default
        /// (the folder that contains the publisher sub-folders). Leave null for the default location.
        /// </summary>
        public static readonly string AssetStoreCacheOverride = null;

        // ─────────────────────────────────────────────────────────────────
        //  Packages
        // ─────────────────────────────────────────────────────────────────

        public static readonly PackageDefinition[] Packages =
        {
            // ── Paid Systems ──────────────────────────────────────────────
            new PackageDefinition
            {
                Id = "text-animator", Name = "Text Animator", Category = "Paid Systems",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/Plugins",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/gui/text-animator-for-unity-ui-toolkit-and-text-mesh-pro-341308",
            },
            new PackageDefinition
            {
                Id = "better-lit-shader", Name = "Better Lit Shader 2021", Category = "Paid Systems",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/Plugins",
                AssetStoreUrl = "https://assetstore.unity.com/packages/vfx/shaders/better-lit-shader-2021-207262",
            },
            new PackageDefinition
            {
                Id = "sonity", Name = "Sonity", Category = "Paid Systems",
                Description = "Audio middleware.",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/Plugins",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/audio/sonity-audio-middleware-229857",
            },
            new PackageDefinition
            {
                Id = "all-in-1-shader-nodes", Name = "All In 1 Shader Nodes", Category = "Paid Systems",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/Plugins",
                AssetStoreUrl = "https://assetstore.unity.com/packages/vfx/shaders/all-in-1-shader-nodes-346742",
            },

            // ── Paid Editor Tools ─────────────────────────────────────────
            new PackageDefinition
            {
                Id = "asset-inventory-4", Name = "Asset Inventory 4", Category = "Paid Editor Tools",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/User",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/utilities/asset-inventory-4-find-every-asset-in-your-library-349582",
            },
            new PackageDefinition
            {
                Id = "vhierarchy-2", Name = "vHierarchy 2", Category = "Paid Editor Tools",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/User",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/utilities/vhierarchy-2-253397",
            },
            new PackageDefinition
            {
                Id = "vfavorites-2", Name = "vFavorites 2", Category = "Paid Editor Tools",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/User",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/utilities/vfavorites-2-263643",
            },
            new PackageDefinition
            {
                Id = "wingman", Name = "Wingman", Category = "Paid Editor Tools",
                Description = "Your inspector's best friend.",
                Source = PackageSource.AssetStore,
                InstallFolder = "Assets/User",
                AssetStoreUrl = "https://assetstore.unity.com/packages/tools/utilities/wingman-your-inspector-s-best-friend-303181",
            },

            // ── Open Source ───────────────────────────────────────────────
            new PackageDefinition
            {
                Id = "nugetforunity", Name = "NuGetForUnity", Category = "Open Source",
                Description = "Temporary helper: installed for NuGet packages, removed again when the run is done.",
                Hidden = true,
                PackageName = "com.glitchenzo.nugetforunity",
                UpmId = "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
            },
            new PackageDefinition
            {
                Id = "zlinq", Name = "ZLinq", Category = "Open Source",
                Description = "Allocation-free LINQ expressions.",
                UpmId = "https://github.com/Cysharp/ZLinq.git?path=src/ZLinq.Unity/Assets/ZLinq.Unity",
                NuGetPackages = new[] { "ZLinq" },
                Requires = new[] { "nugetforunity" },
            },
            new PackageDefinition
            {
                Id = "r3", Name = "R3", Category = "Open Source",
                Description = "Reactive programming on steroids.",
                UpmId = "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
                NuGetPackages = new[] { "R3" },
                Requires = new[] { "nugetforunity" },
            },
            new PackageDefinition
            {
                Id = "litmotion", Name = "LitMotion", Category = "Open Source",
                Description = "Allocation-free tween library.",
                UpmId = "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
            },
            new PackageDefinition
            {
                Id = "saintsfield", Name = "Saints Field", Category = "Open Source",
                Description = "Editor attribute library.",
                UpmId = "https://github.com/TylerTemp/SaintsField.git",
            },
        };
    }
}
