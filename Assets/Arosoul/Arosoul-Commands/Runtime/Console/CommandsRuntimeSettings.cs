using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arosoul.Commands
{
    /// <summary>
    /// Runtime copy of the console settings, baked to Assets/Generated/Commands and added to the preloaded assets.
    /// Do not edit it directly; use Tools > Arosoul > Commands > Settings.
    /// </summary>
    public sealed class CommandsRuntimeSettings : ScriptableObject
    {
        [SerializeField] CommandsSettingsData _data = new();

        private static CommandsRuntimeSettings _instance;
        private static CommandsSettingsData _defaults;

        public CommandsSettingsData Data => _data;

        /// <summary>
        /// The baked settings, or null if they have not been baked.
        /// </summary>
        public static CommandsRuntimeSettings Instance
        {
            get
            {
                if (_instance == null) _instance = Find();
                return _instance;
            }
        }

        /// <summary>
        /// The baked settings, or defaults if they have not been baked.
        /// </summary>
        public static CommandsSettingsData CurrentData => Instance != null ? Instance.Data : _defaults ??= new CommandsSettingsData();

        /// <summary>
        /// True if the console may run in the current editor or build.
        /// </summary>
        public static bool IsAvailable(CommandsAvailability availability)
        {
            return Application.isEditor ||
                   availability == CommandsAvailability.AllBuilds ||
                   (availability == CommandsAvailability.DevelopmentBuilds && Debug.isDebugBuild);
        }

        internal void SetData(CommandsSettingsData data)
        {
            _data = data;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private static CommandsRuntimeSettings Find()
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(CommandsRuntimeSettings)}");
            foreach (string guid in guids)
            {
                var settings = AssetDatabase.LoadAssetAtPath<CommandsRuntimeSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null) return settings;
            }
            return null;
#else
            CommandsRuntimeSettings[] loaded = Resources.FindObjectsOfTypeAll<CommandsRuntimeSettings>();
            return loaded.Length > 0 ? loaded[0] : null;
#endif
        }
    }
}
