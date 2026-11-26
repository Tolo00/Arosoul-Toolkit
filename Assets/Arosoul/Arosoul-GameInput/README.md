# Arosoul GameInput

A thin wrapper on the Unity Input System for singleplayer games.
Edit actions in Unity's own **Input Actions editor** and `GameActions` is generated for you.

## Setup
1. `Tools > Game Input > Create Config` (creates `Assets/Resources/GameInputConfig.asset`).
2. Assign your Input Actions asset to it. `GameActions.cs` is generated into `Assets/Generated` (changeable) and regenerated every time the asset is saved.
3. Use it (`GameActions` is generated in the global namespace; add `using Arosoul.GameInput;` for `GameInput` and `GameInputBindings`):
   ```csharp
   var move = GameActions.Player.Move.Value;                 // Vector2Action
   if (GameActions.Player.Jump.WasPressedThisFrame) { }      // ButtonAction
   float scroll = GameActions.Player.Scroll.Value;           // AnalogAction
   var reg = GameActions.Player.BackPriority.Register(ctx => CloseMenu(), priority: 10);   // PriorityAction
   GameActions.Player.Disable();                             // per map
   string prompt = GameActions.Player.Jump.GetBindingLabel(); // "Space" / "A" / "Cross" depending on the active device
   ```

## Action types (set in the Input Actions editor)
| In the asset | Generated type |
|---|---|
| name ends with `Priority` (e.g. `BackPriority`) | `PriorityAction` |
| expected control type `Vector2` | `Vector2Action` |
| expected control type `Axis` / `Analog` | `AnalogAction` |
| `Button` / not set | `ButtonAction` |

Maps listed under *Excluded Maps* in the config (default `UI`) are not wrapped.

## Gamepad
* `GameInput.ActiveDevice`, `OnActiveDeviceChanged`: switches on any real input (stick drift and mouse jitter ignored) and falls back to keyboard when the pad disconnects.
* `GameInput.ActiveGamepadKind` (Xbox / PlayStation / Switch / Generic) and `OnGamepadKindChanged` for prompt icons.
* `GameInput.Rumble(low, high, duration)`, `StopRumble()`, `RumbleEnabled`.
* Mouse hiding on gamepad use, as before (`SetMouseVisibilityPreference`).

## Remapping
`GameInputBindings`: `GetEntries(map, device)`, `StartRebind`, `CancelRebind`, `ResetBinding`, `ResetMap`, `SaveMap`, `GetMapJson` / `RestoreMapJson`.
Keyboard bindings can only become keyboard/mouse controls and gamepad bindings gamepad controls. Mouse look/scroll are never remappable,
more can be locked in the config. Saved to `input-bindings.json` per map. With *Swap* conflicts, taking a control that another action uses gives that action your old one.
Cancel with Escape (keyboard) or Start (gamepad), configurable. The Player Settings integration builds the UI for this.

## Automation
`Arosoul.GameInput.EditorTools.GameInputAgentTools` creates and validates the config, adds actions and bindings, saves the Input Actions asset and generates `GameActions` from code. See SKILLS.md.
