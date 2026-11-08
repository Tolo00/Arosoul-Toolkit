using Arosoul.Essentials;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    [RequireComponent(typeof(PlayerInputManager))]
    public partial class InputManager : SingletonPersistent<InputManager> {
        private GlobalActions _globalActions; 
        public static GlobalActions GlobalActions => Inst._globalActions;

        // Registered Buttons
        public static ButtonWrapper GlobalPauseButton = new();
        public static ButtonWrapper GlobalBackButton = new();



        private void RegisterGlobalActions() {
            RegisterWrapper(GlobalActions.Player.Pause, GlobalPauseButton);
            RegisterWrapper(GlobalActions.Player.Back, GlobalBackButton);
        }





    #region Setup/Cleanup

        internal void SetupGlobalActions() {
            _globalActions = new GlobalActions();
            _globalActions.Enable();

            RegisterGlobalActions();
        }

        internal void CleanupGlobalActions() {
            _globalActions.Disable();
            _globalActions.Dispose();
        }

    #endregion
    }
}