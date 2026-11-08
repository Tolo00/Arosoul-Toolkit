using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
#endif

namespace Arosoul.SceneSystem 
{
    public class SceneSystemSettings : ScriptableObject
    {
        public const string SettingsPath = "Assets/Settings/SceneSystemSettings.asset";
        public const string SettingsDirectory = "Assets/Settings";
        
        [SerializeField]
        [Tooltip("Name of the core scene to load at startup.")]
        private string _coreSceneName = "Core";
        
        [SerializeField]
        [Tooltip("If enabled, the core scene will automatically load on startup. Disable this only if you want to manually control or disable core scene loading.")]
        private bool _loadCoreSceneOnStartup = false;

        [SerializeField]
        [Tooltip("The slot the first loaded scene should occupy.")]
        private string _initialSceneSlot = "Main";

        [SerializeField]
        [Tooltip("Registered transitions for the transition system.")]
        private List<TransitionEntry> _registeredTransitions = new();
        
        public string CoreSceneName => _coreSceneName;
        public bool LoadCoreSceneOnStartup => _loadCoreSceneOnStartup;

        public string InitialSceneSlot => _initialSceneSlot;

        public IReadOnlyList<TransitionEntry> RegisteredTransitions => _registeredTransitions;
        
        private static SceneSystemSettings _instance;
        public static SceneSystemSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    #if UNITY_EDITOR
                        _instance = GetOrCreateSettings();
                    #else
                        Debug.LogError("SceneSystemSettings not found in preloaded assets!");
                    #endif
                }
                return _instance;
            }
        }

        void OnEnable()
        {
            if (_instance == null)
                _instance = this;
        }

#if UNITY_EDITOR
        internal static SceneSystemSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SceneSystemSettings>(SettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<SceneSystemSettings>();
                System.IO.Directory.CreateDirectory(SettingsDirectory);
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
                
                AddToPreloadedAssets(settings);
            }
            return settings;
        }
        
        internal static void AddToPreloadedAssets(SceneSystemSettings settings)
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (!System.Array.Exists(preloadedAssets, asset => asset is SceneSystemSettings))
            {
                var newPreloadedAssets = new Object[preloadedAssets.Length + 1];
                preloadedAssets.CopyTo(newPreloadedAssets, 0);
                newPreloadedAssets[preloadedAssets.Length] = settings;
                PlayerSettings.SetPreloadedAssets(newPreloadedAssets);
            }
        }
        
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
        #endif
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Ensures the SceneSystemSettings scriptable object is preloaded and accessible in build.
    /// </summary>
    public class PreloadedAssetsBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = SceneSystemSettings.GetOrCreateSettings();
            SceneSystemSettings.AddToPreloadedAssets(settings);
        }
    }
    #endif
}