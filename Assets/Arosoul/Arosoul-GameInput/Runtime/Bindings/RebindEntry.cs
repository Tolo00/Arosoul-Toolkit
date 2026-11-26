using UnityEngine.InputSystem;

namespace Arosoul.GameInput {

    /// <summary>One remappable binding (a single control assigned to an action), as listed by GameInputBindings.GetEntries.</summary>
    public sealed class RebindEntry {
        public string MapName { get; internal set; }

        /// <summary>Name without the " #2" suffix. Entries of the same action and BaseName are alternatives for the same input.</summary>
        public string BaseName { get; internal set; }
        
        public InputAction Action { get; internal set; }

        /// <summary>Index in Action.bindings.</summary>
        public int BindingIndex { get; internal set; }

        public GameInput.DeviceType Device { get; internal set; }

        /// <summary>"Jump", "Move Up", "Interact #2"...</summary>
        public string DisplayName { get; internal set; }
    }

}
