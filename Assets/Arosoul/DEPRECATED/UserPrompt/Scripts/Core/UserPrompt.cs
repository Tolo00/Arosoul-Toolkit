using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arosoul.Tools {

    public static class UserPrompt {
        
        public static ConfirmPromptWindow ShowConfirmPrompt(Action confirmCallback = null, 
                                            Action cancelCallback = null,
                                            Action completedCallback = null, 
                                            string promptTitle = "Confirm action?", 
                                            string promptSubtitle = "", 
                                            string confirmText = "Confirm", 
                                            string cancelText = "Cancel",
                                            int sortOrder = 100,
                                            ConfirmPromptWindow customWindowPrefab = null,
                                            Vector2? anchoredPos = null) 
        {
            ConfirmPromptWindow prompt = 
                CreateUserPromptWindow(customWindowPrefab, sortOrder, anchoredPos);

            // Setup prompt
            prompt.OnConfirmAction += confirmCallback;
            prompt.OnCanceledAction += cancelCallback;
            prompt.OnCompletedAction += completedCallback;

            prompt.TitleText.text = promptTitle;
            prompt.SubtitleText.text = promptSubtitle;

            prompt.ConfirmButtonText.text = confirmText;
            prompt.CancelButtonText.text = cancelText;

            prompt.Setup();
            return prompt;
        }

        public static InfoPromptWindow ShowInfoPrompt(
                                            Action completedCallback = null, 
                                            string promptTitle = "Info Prompt", 
                                            string promptSubtitle = "", 
                                            string continueText = "Continue", 
                                            int sortOrder = 100,
                                            InfoPromptWindow customWindowPrefab = null,
                                            Vector2? anchoredPos = null) 
        {
            InfoPromptWindow prompt = 
                CreateUserPromptWindow(customWindowPrefab, sortOrder, anchoredPos);

            // Setup prompt
            prompt.OnCompletedAction += completedCallback;

            prompt.TitleText.text = promptTitle;
            prompt.SubtitleText.text = promptSubtitle;

            prompt.ContinueButtonText.text = continueText;

            prompt.Setup();
            return prompt;
        }



        private static T CreateUserPromptWindow<T>(T customWindowPrefab = null, 
                                                int sortOrder = 10, 
                                                Vector2? anchoredPosition = null) where T : UserPromptWindow<T> 
        {
            Transform canvas = CreateCanvas(typeof(T), sortOrder);

            T promptInstance;
            if (customWindowPrefab != null) promptInstance = GameObject.Instantiate(customWindowPrefab);
            else promptInstance = LoadDefaultPromptFromResources<T>();

            promptInstance.transform.SetParent(canvas);
            promptInstance.canvas = canvas;

            // Set anchored position
            Vector2 pos = anchoredPosition == null ? Vector2.zero : (Vector2)anchoredPosition;
            promptInstance.GetComponent<RectTransform>().anchoredPosition = pos;
            
            return promptInstance;
        }

        /// <summary>
        /// Load default prompt instance. Search Path: Resources/UserPromptWindow/
        /// </summary>
        private static T LoadDefaultPromptFromResources<T>() where T : UserPromptWindow<T> {
            T[] prefabs = Resources.LoadAll<T>("UserPromptWindow"); // Path: Resources/UserPromptWindow/

            foreach (var prefab in prefabs) {
                if (prefab != null) {
                    T instance = GameObject.Instantiate(prefab);
                    instance.gameObject.name = $"{prefab.name}";
                    return instance;
                }
            }

            return null; // No matching prefab found
        }

        private static Transform CreateCanvas(Type userPrompt, int sortOrder) {
            GameObject go = new GameObject(userPrompt.Name);
            
            Canvas canvas;
            CanvasScaler canvasScaler;

            canvas = go.AddComponent<Canvas>();            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder; // High sorting order so it appears on top
            
            canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            go.AddComponent<GraphicRaycaster>();

            return go.transform;
        }
    }

}