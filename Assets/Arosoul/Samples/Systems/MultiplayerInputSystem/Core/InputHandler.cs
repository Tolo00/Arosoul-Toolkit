using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public class InputHandler : MonoBehaviour {
        
        private InputManager _inputManager;

        public PlayerInput PlayerInput { get; private set; }
        public int PlayerIndex => PlayerInput.playerIndex;


        internal void InitializeHandler() {
            // Get references
            PlayerInput = GetComponent<PlayerInput>();
            _inputManager = InputManager.Inst;

            Debug.Assert(PlayerInput != null, "Missing PlayerInput component.");
            Debug.Assert(_inputManager != null, "Missing Input Manager in scene.");
            
            // Initialize
            transform.SetParent(_inputManager.transform);
            UpdateDisplayName();

            // Events Callback
            PlayerInput.onDeviceLost += PlayerInput_OnDeviceLost;
        }

        private void OnDestroy() {
            // Clean-up
            if (PlayerInput != null) {
                PlayerInput.onDeviceLost -= PlayerInput_OnDeviceLost;
            }
        }

    #region Public API

        public void RemovePlayer() => StartCoroutine(DelayedPlayerRemoval());    

    #endregion

    #region Callbacks

        private void PlayerInput_OnDeviceLost(PlayerInput input) {
            Debug.Log($"Device Lost: {gameObject.name}", this);
            RemovePlayer();
        }

    #endregion

        private void UpdateDisplayName() {
            name = $"InputHandler ({PlayerInput.currentControlScheme}, {PlayerInput.playerIndex})";
        }

        private bool _isRemoving = false;
        private IEnumerator DelayedPlayerRemoval() {
            if (_isRemoving) yield break;
            _isRemoving = true;

            yield return new WaitForEndOfFrame();
            PlayerInput.user.UnpairDevicesAndRemoveUser();
            Destroy(gameObject);
        }
    }
}