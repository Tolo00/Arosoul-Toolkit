using System;
using UnityEngine.InputSystem;

namespace GameInputSystem {

    /// <summary>
    /// One action from the Input Actions asset. Instances are created by the generated GameActions class and
    /// linked to the real InputAction automatically at startup (by map and action name).
    /// </summary>
    public abstract class GameAction {
        readonly Action<InputAction.CallbackContext> callback;

        public string MapName { get; }
        public string ActionName { get; }

        /// <summary>"Map/Action", the path used by InputActionAsset.FindAction.</summary>
        public string Path => MapName + "/" + ActionName;

        /// <summary>The underlying action. Null until GameInput has initialised.</summary>
        public InputAction InputAction { get; private set; }

        protected GameAction(string mapName, string actionName) {
            MapName = mapName;
            ActionName = actionName;
            callback = OnCallback;
        }

        /// <summary>Label of this action's binding for a device, e.g. "Space", "Cross", "W/A/S/D". Use it for prompts.</summary>
        public string GetBindingLabel(GameInput.DeviceType? device = null) {
            return GameInputBindings.GetActionLabel(this, device ?? GameInput.ActiveDevice);
        }

        internal void Bind(InputAction action) {
            Unbind();
            InputAction = action;
            ResetState();

            action.started += callback;
            action.performed += callback;
            action.canceled += callback;
        }

        internal void Unbind() {
            if (InputAction == null) return;
            InputAction.started -= callback;
            InputAction.performed -= callback;
            InputAction.canceled -= callback;
            InputAction = null;
        }

        void OnCallback(InputAction.CallbackContext ctx) => Invoke(ctx);

        internal abstract void Invoke(InputAction.CallbackContext ctx);

        /// <summary>Called at the end of every frame, clears the "this frame" flags.</summary>
        internal abstract void ResetFrameState();

        /// <summary>Called when the action is (re)bound, clears everything.</summary>
        internal virtual void ResetState() => ResetFrameState();
    }

}
