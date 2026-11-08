using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public interface IInputReceiver {
        int PlayerIndex { get; }

        void OnRegisterInput(PlayerInput input);
        void OnDeregisterInput(PlayerInput input);
    }
}