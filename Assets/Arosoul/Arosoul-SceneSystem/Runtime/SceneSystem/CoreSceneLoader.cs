using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arosoul.SceneSystem
{
    public class CoreSceneLoader
    {
        static EmptyMonoBehaviour _tempMono;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadCoreScene()
        {
            var settings = SceneSystemSettings.Instance;
            if (!settings.LoadCoreSceneOnStartup) return;
            
            string coreSceneName = settings.CoreSceneName;
            if (string.IsNullOrEmpty(coreSceneName)) return;

            Scene coreScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(coreSceneName);
            if (!coreScene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(coreSceneName, LoadSceneMode.Additive);

                // Create a temporary object to run the coroutine
                _tempMono = new GameObject("[CoreSceneLoader-Temp]").AddComponent<EmptyMonoBehaviour>();
                _tempMono.StartCoroutine(RegisterCoreSceneAfterLoad(coreSceneName));
            }
        }

        static IEnumerator RegisterCoreSceneAfterLoad(string coreSceneName)
        {
            yield return null;
            
            var field = typeof(SceneManager).GetField("_loadedSceneBySlot", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) 
                    ?? throw new System.Exception("Unable to find _loadedSceneBySlot using reflection.");
            
            
            Dictionary<string, string> loadedScenes = field.GetValue(SceneManager.Inst) as Dictionary<string, string>;
            loadedScenes.Add("Core", coreSceneName);
            
            GameObject.Destroy(_tempMono.gameObject);
        }
    }
}
