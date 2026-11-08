using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arosoul.Tools {

    public class InfoPromptWindow : UserPromptWindow<InfoPromptWindow> {

        internal override void Setup() {
            TitleText.gameObject.SetActive(TitleText.text != "");
            SubtitleText.gameObject.SetActive(SubtitleText.text != "");

            gameObject.SetActive(true);
            if (EventSystem.current == null) Debug.LogWarning("No EventSystem in scene. User Prompt will not work as expected.");
            else EventSystem.current.SetSelectedGameObject(startSelectionOnShow);
        }

        [SerializeField] GameObject startSelectionOnShow;
        
        [Space]

        // UI fields
        public TMP_Text TitleText;
        public TMP_Text SubtitleText;
        [Space]
        public TMP_Text ContinueButtonText;


        // Callbacks
        internal Action OnCompletedAction;


        public void TriggerContinueAction() { 
            OnCompletedAction?.Invoke();
            CleanUp();
        }
    }

}