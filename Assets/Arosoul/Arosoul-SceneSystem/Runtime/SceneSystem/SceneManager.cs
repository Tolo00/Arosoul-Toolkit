using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arosoul.SceneSystem 
{
    public class SceneManager : LazyLoaded<SceneManager>
    {
        private Dictionary<string, string> _loadedSceneBySlot = new();
        public bool IsBusy { get; private set; }

        protected override void OnSingletonEnable()
        {
            // Place first loaded scene into a slot
            _loadedSceneBySlot.Add(SceneSystemSettings.Instance.InitialSceneSlot, GetActiveName);
        }

    #region API

        public static bool IsLoadingScene => Inst.IsBusy;

        public static int GetActiveBuildIndex => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        public static string GetActiveName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;    

        /// <summary>
        /// Check if a slot contains a loaded scene.
        /// </summary>
        public static bool IsSlotLoaded(string slotKey)
        {
            return Inst._loadedSceneBySlot.ContainsKey(slotKey);
        }

        /// <summary>
        /// Check if scene is loaded.
        /// </summary>
        /// <param name="sceneName">Name of scene to check.</param>
        /// <param name="slotKey">The slot which contains the loaded scene.</param>
        public static bool IsSceneLoaded(string sceneName, out string slotKey)
        {
            if (Inst._loadedSceneBySlot.ContainsValue(sceneName))
            {
                foreach (var kvp in Inst._loadedSceneBySlot)
                {
                    if (kvp.Value == sceneName)
                    {
                        slotKey = kvp.Key;
                        return true;
                    }
                }
            }

            slotKey = "";
            return false;
        }

        public static SceneOperation NewOperation() 
        {
            return new SceneOperation();
        }


    #endregion

        
        internal Coroutine ExecuteOperation(SceneOperation operation) 
        {
            if (IsBusy)
            {
                Debug.LogWarning("Scene operation already in progress.");
                return null;
            }
            
            IsBusy = true;
            return StartCoroutine(SceneOperationRoutine(operation));
        }

        IEnumerator SceneOperationRoutine(SceneOperation operation)
        {
            bool onInCompleted = true;
            bool onOutCompleted = true;
            bool onWaitCompleted = true;

            TransitionOperation transitionOp = null;

            // Transition in
            if (operation.TransitionParameters != null)
            {
                var parameters = operation.TransitionParameters;
                transitionOp = TransitionAPI.StartTransition(operation.TransitionParameters, 1000);
                
                transitionOp.WaitForOutPermission();

                onInCompleted = false;
                onOutCompleted = false;
                onWaitCompleted = false;

                transitionOp.OnInCompleted += () => onInCompleted = true;
                transitionOp.OnOutCompleted += () => onOutCompleted = true;
                transitionOp.OnWaitCompleted += () => onWaitCompleted = true;
            }

            // Wait for in transition to finish
            while (!onInCompleted)
                yield return null;

            // Unload scenes
            foreach (string slotKey in operation.ScenesToUnload)
            {
                yield return UnloadSceneRoutine(slotKey);
            }

            // Load scenes
            foreach (var kvp in operation.ScenesToLoad)
            {
                if (_loadedSceneBySlot.ContainsKey(kvp.Key))
                {
                    yield return UnloadSceneRoutine(kvp.Key);
                }
                yield return LoadAdditiveRoutine(kvp.Key, kvp.Value, operation.ActiveScene == kvp.Value);
            }

            if (operation.UnloadUnusedAssets)
                yield return UnloadUnusedAssetsRoutine();

            // Wait for min transition time to finish
            while (!onWaitCompleted)
                yield return null;

            // Transition out
            transitionOp?.GiveOutPermission();

            // Wait for out transition to finish
            while (!onOutCompleted)
                yield return null;

            operation.OnOperationComplete?.Invoke();
            IsBusy = false;
        }

        IEnumerator LoadAdditiveRoutine(string slotKey, string sceneName, bool setActive)
        {
            AsyncOperation loadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp == null) yield break;
            
            loadOp.allowSceneActivation = false;
            while (loadOp.progress < 0.9f)
            {
                yield return null;
            }

            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone)
            {
                yield return null;
            }

            if (setActive)
            {
                Scene newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (newScene.IsValid() && newScene.isLoaded)
                {
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(newScene);
                }
            }

            _loadedSceneBySlot[slotKey] = sceneName;
        }

        IEnumerator UnloadSceneRoutine(string slotKey)
        {
            if (!_loadedSceneBySlot.TryGetValue(slotKey, out string sceneName)) yield break;
            if (string.IsNullOrEmpty(sceneName)) yield break;

            AsyncOperation unloadOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone) 
                    yield return null;
            }

            _loadedSceneBySlot.Remove(slotKey);
        }

        IEnumerator UnloadUnusedAssetsRoutine()
        {
            AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
            while (!cleanupOp.isDone)
                yield return null;
        }
    }
}