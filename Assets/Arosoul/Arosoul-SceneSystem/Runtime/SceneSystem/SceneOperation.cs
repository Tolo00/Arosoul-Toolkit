using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.SceneSystem
{
    public class SceneOperation {
        internal Dictionary<string, string> ScenesToLoad = new();
        internal List<string> ScenesToUnload = new();

        internal string ActiveScene = "";
        internal bool UnloadUnusedAssets = false;
        
        internal TransitionParameters TransitionParameters = null;

        
    #region Builder API

        public Action OnOperationComplete;

        public SceneOperation Load(string sceneKey, string sceneName, bool setActive)
        {
            if (sceneKey == "Core")
            {
                Debug.LogWarning("Cannot load scene into Core slot.");
                return this;
            }

            ScenesToLoad[sceneKey] = sceneName;
            if (setActive) ActiveScene = sceneName;
            return this;
        }

        public SceneOperation Unload(string sceneKey)
        {
            if (sceneKey == "Core")
            {
                Debug.LogWarning("Cannot unload Core scene.");
                return this;
            }

            ScenesToUnload.Add(sceneKey);
            return this;
        }

        public SceneOperation WithUnloadUnusedAssets()
        {
            UnloadUnusedAssets = true;
            return this;
        }

        public SceneOperation WithTransition(TransitionParameters parameters)
        {
            TransitionParameters = parameters;
            return this;
        }  
        
        public void Perform() {
            SceneManager.Inst.ExecuteOperation(this);
        }

    #endregion
    }
}
