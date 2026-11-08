using UnityEngine.InputSystem;

namespace GameInputSystem {
    public abstract class GameAction {
        internal abstract void Invoke(InputAction.CallbackContext ctx);
        internal abstract void ResetFrameState();
    }
}