using UnityEngine;

namespace GameInputSystem 
{
    public partial class GameInput : MonoBehaviour {

        /// <summary>
        /// Automatically hide mouse when active device switches to Gamepad.
        /// Mouse will only be visible while active DeviceType is set to Keyboard.
        /// </summary>
        internal readonly bool HideMouseOnGamepadUse = true;
        

        internal static bool VerboseLogging = false;
    }
}