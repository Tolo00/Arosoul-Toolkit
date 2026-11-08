using UnityEngine;

namespace GameInputSystem 
{
    public partial class GameInput : MonoBehaviour {
        // Vector Actions
        public static Vector2Action MoveAction { get; private set; } = new();
        public static Vector2Action LookAction { get; private set; } = new();
        
        // Analog Actions
        public static AnalogAction ScrollAction { get; private set; } = new();

        // Button Actions
        public static ButtonAction PrimaryAction { get; private set; } = new();
        public static ButtonAction SecondaryAction { get; private set; } = new();

        public static ButtonAction InteractAction { get; private set; } = new();
        public static ButtonAction JumpAction { get; private set; } = new();

        // Priority Actions
        public static PriorityAction BackPriorityAction { get; private set; } = new();

        internal void RegisterActions() {
            // Vector2 Actions
            RegisterAction(_inputActions.Player.Move, MoveAction);
            RegisterAction(_inputActions.Player.Look, LookAction);

            // Analog Actions
            RegisterAction(_inputActions.Player.Scroll, ScrollAction);

            // Button Actions
            RegisterAction(_inputActions.Player.Primary, PrimaryAction);
            RegisterAction(_inputActions.Player.Secondary, SecondaryAction);
            RegisterAction(_inputActions.Player.Interact, InteractAction);

            // Priority Actions
            RegisterAction(_inputActions.Player.Back, BackPriorityAction);
        }
    }
}