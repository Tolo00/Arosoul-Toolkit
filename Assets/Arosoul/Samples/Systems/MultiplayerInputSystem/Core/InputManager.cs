using System;
using System.Collections.Generic;
using Arosoul.Essentials;
using UnityEngine;
using UnityEngine.InputSystem;

/// TODO:
/// - Implement UI for individual players

namespace MultiplayerInputSystem
{
    [RequireComponent(typeof(PlayerInputManager))]
    public partial class InputManager : SingletonPersistent<InputManager> {
        
        PlayerInputManager _playerInputManager; // Handle mutliplayer input

        Dictionary<int, InputHandler> _indexToHandlerLookup = new();

        private List<IInputReceiver> _inputReceivers = new();
        private List<IInputActionWrapper> _activeWrappers = new(); // Used to reset input state at end of frame.

        protected override void OnSingletonEnable() {
            _playerInputManager = GetComponent<PlayerInputManager>(); // Multiplayer manager

            SetupGlobalActions();
            
            // Events Callback
            _playerInputManager.onPlayerJoined += PlayerInputManager_OnPlayerJoined;
            _playerInputManager.onPlayerLeft += PlayerInputManager_OnPlayerLeft;  
        }

        protected override void OnSingletonDisable() {
            CleanupGlobalActions();
        }

        void LateUpdate() {
            ResetActions();
        }

        private void ResetActions() {
            foreach (IInputActionWrapper action in _activeWrappers) {
                action.ResetStateEndOfFrame();
            }
        }

    #region Static API

        // Events
        public static Action<PlayerInput> OnPlayerJoined;
        public static Action<PlayerInput> OnPlayerLeft;

        public static Action<IInputReceiver> OnReceiverRegistered;
        public static Action<IInputReceiver> OnReceiverDeregistered;

        public static bool JoiningEnabled {
            get { return Inst._playerInputManager.joiningEnabled; }
            set { 
                if (value) Inst._playerInputManager.EnableJoining();
                else Inst._playerInputManager.DisableJoining();
            }
        } 

        public static bool PreventKeyboardJoining { get; set; }

        public static int PlayerCount => Inst._indexToHandlerLookup.Count;

        public static bool PlayerIndexActive(int index) {
            return Inst._indexToHandlerLookup.ContainsKey(index);
        }

        public static void RemovePlayer(PlayerInput playerInput) {
            Inst._indexToHandlerLookup[playerInput.playerIndex].RemovePlayer();
        }

        public static void RemovePlayer(int playerIndex) {
            Inst._indexToHandlerLookup[playerIndex].RemovePlayer();
        }

        [ContextMenu("Remove All Players")]
        public void RemoveAllPlayersDebug() => RemoveAllPlayers();
        public static void RemoveAllPlayers() {
            foreach (var kvp in Inst._indexToHandlerLookup) {
                kvp.Value.RemovePlayer();
            }
        }

        public static void RegisterInputReceiver(IInputReceiver inputReceiver) {
            Inst._inputReceivers.Add(inputReceiver);

            // Register wrapper
            if (Inst._indexToHandlerLookup.ContainsKey(inputReceiver.PlayerIndex)) {
                inputReceiver.OnRegisterInput(Inst._indexToHandlerLookup[inputReceiver.PlayerIndex].PlayerInput);
            }

            OnReceiverRegistered?.Invoke(inputReceiver);
        }

        public static void DeregisterInputReceiver(IInputReceiver inputReceiver) {
            Inst._inputReceivers.Remove(inputReceiver);

            // Deregister wrapper
            if (Inst._indexToHandlerLookup.ContainsKey(inputReceiver.PlayerIndex)) {
                inputReceiver.OnDeregisterInput(Inst._indexToHandlerLookup[inputReceiver.PlayerIndex].PlayerInput);
            }

            OnReceiverDeregistered?.Invoke(inputReceiver);
        }

        public static void RegisterWrapper(InputAction inputAction, IInputActionWrapper wrapper) {
            inputAction.started += context => wrapper.UpdateState(context);
            inputAction.performed += context => wrapper.UpdateState(context);
            inputAction.canceled += context => wrapper.UpdateState(context);

            wrapper.InputAction = inputAction;
            Inst._activeWrappers.Add(wrapper);
        }

        public static void DeregisterWrapper(InputAction inputAction, IInputActionWrapper wrapper) {
            inputAction.started += context => wrapper.UpdateState(context);
            inputAction.performed += context => wrapper.UpdateState(context);
            inputAction.canceled += context => wrapper.UpdateState(context);

            wrapper.ResetStateEndOfFrame();
            wrapper.InputAction = null;
            Inst._activeWrappers.Remove(wrapper);
        }

    #endregion

    #region Callbacks

        private void PlayerInputManager_OnPlayerJoined(PlayerInput playerInput) {
            print($"Player Joined: {playerInput}");
            InputHandler inputHandler = playerInput.GetComponent<InputHandler>();
            inputHandler.InitializeHandler();

            RegisterReceiversToHandler(inputHandler);
            _indexToHandlerLookup.Add(playerInput.playerIndex, inputHandler);

            OnPlayerJoined?.Invoke(playerInput);

            // Prevent keyboard from joining
            if (PreventKeyboardJoining) {
                var device = playerInput.devices[0];
                if (device is Keyboard || device is Mouse) {
                    RemovePlayer(playerInput);
                }
            }
        } 

        private void PlayerInputManager_OnPlayerLeft(PlayerInput playerInput) {
            print($"Player Left: {playerInput}");
            InputHandler inputHandler = playerInput.GetComponent<InputHandler>();

            DeregisterReceiversToHandler(inputHandler);
            _indexToHandlerLookup.Remove(playerInput.playerIndex);

            OnPlayerLeft?.Invoke(playerInput);
        }

    #endregion

        private void RegisterReceiversToHandler(InputHandler inputHandler) {
            foreach (IInputReceiver inputReceiver in _inputReceivers) {
                if (inputReceiver.PlayerIndex == inputHandler.PlayerIndex) {
                    inputReceiver.OnRegisterInput(inputHandler.PlayerInput);
                }
            }
        }

        private void DeregisterReceiversToHandler(InputHandler inputHandler) {
            foreach (IInputReceiver inputReceiver in _inputReceivers) {
                if (inputReceiver.PlayerIndex == inputHandler.PlayerIndex) {
                    inputReceiver.OnDeregisterInput(inputHandler.PlayerInput);
                }
            }
        }


        /// <summary>
        /// Ensure anonymous function throws exceptions in the console.
        /// </summary>
        private void ExecuteAction(System.Action action, InputAction.CallbackContext context) {
            try {
                action?.Invoke();
            }
            catch (System.Exception ex) {
                throw ex;
            }
        }

        internal static void PrintWrapperFixedUpdateWarning(string wrapperName, string propName) {
            Debug.LogWarning(
                $"Property ({propName}) in Wrapper ({wrapperName}) was accessed during FixedUpdate(). " +
                "This property is only valid inside Unity's Update callback."
            );
        }
    }
}