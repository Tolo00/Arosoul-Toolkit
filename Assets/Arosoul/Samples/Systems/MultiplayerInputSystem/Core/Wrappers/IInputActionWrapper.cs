using UnityEngine.InputSystem;

namespace MultiplayerInputSystem
{
    public interface IInputActionWrapper {
        InputAction InputAction { get; set; }

        void ResetStateEndOfFrame();
        void UpdateState(InputAction.CallbackContext context);
    }
}