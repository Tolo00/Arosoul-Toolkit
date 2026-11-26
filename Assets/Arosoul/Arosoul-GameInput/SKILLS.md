# Skill: Arosoul GameInput (`Arosoul.GameInput`)

## Purpose and boundaries

A small single-player wrapper around the **Unity Input System**. The project keeps its real input definitions in a Unity `.inputactions` asset; this system generates a typed C# facade named `GameActions` and supplies device detection, mouse visibility, rumble, prompt labels, and binding remapping. It requires the Unity Input System package (`Unity.InputSystem` assembly).

Namespace: `Arosoul.GameInput`  
Editor automation namespace: `Arosoul.GameInput.EditorTools`  
Runtime assembly: `Arosoul.GameInput`  
Editor assembly: `Arosoul.GameInput.Editor`  
Generated facade: `GameActions` in the **global namespace** (like `SceneIds` and `TransitionIds`); game code needs `using Arosoul.GameInput;` only for `GameInput`, `GameInputBindings` and the wrapper types.

Earlier versions were named `GameInputSystem` (namespace and assemblies). To upgrade a project: replace `using GameInputSystem;` with `using Arosoul.GameInput;`, change asmdef references from `GameInputSystem` to `Arosoul.GameInput`, and in the old generated `GameActions.cs` replace `namespace GameInputSystem` with `namespace Arosoul.GameInput` by hand. The project must compile before the new generator is loaded; after that, `GenerateActions` rewrites the file in the global namespace.

**Name collision:** the static class `GameInput` lives in the namespace `Arosoul.GameInput`. Code in the global namespace or in `Arosoul.GameInput` can write `GameInput.ActiveDevice`. Code inside any other `Arosoul.*` namespace resolves `GameInput` to the namespace instead; there, add `using GameInput = Arosoul.GameInput.GameInput;` **inside** the namespace block (as `Arosoul.PlayerSettings`' `ActionMapSection` does) or fully qualify the class.

This is not a multiplayer/player-input ownership system. It assumes one local player and one active input context.

## Architecture and startup

1. `GameInputConfig` is a `ScriptableObject` that references an `InputActionAsset` and stores device, rebinding, and generation options.
2. The config must be named **`GameInputConfig.asset` directly under a `Resources` folder** (for example `Assets/Resources/GameInputConfig.asset`). Runtime loading calls `Resources.Load<GameInputConfig>("GameInputConfig")`; a `Resources` subfolder or a renamed asset will not be found.
3. The Editor generator writes `GameActions.cs` into the configured generated-code folder (default `Assets/Generated`).
4. The generated static class registers its action wrappers at subsystem initialization.
5. Before the first scene loads, `GameInputRuntime` loads the config, instantiates a **runtime copy** of the Input Actions asset, links wrappers by `MapName/ActionName`, loads saved binding overrides, enables the asset, and creates a hidden persistent runner.

The runtime copy is important: rebinding changes the runtime copy and does not modify the source `.inputactions` asset. The package can be imported directly under `Assets`; keep its `Runtime`, `Editor`, assembly-definition, and `.meta` files together.

## Agent rules

- **Do not edit `GameActions.cs`.** It is generated. Change the `.inputactions` asset, then regenerate.
- Prefer `GameInputAgentTools.AddAction`/`AddBinding`/`AddVector2Composite`/`RemoveAction` + `SaveActions` (or the Unity Input Actions editor). Do not hand-edit the serialized `.inputactions` JSON.
- Keep one `GameInputConfig` in the project. `Resources.Load` expects a unique config named exactly `GameInputConfig`. `ValidateConfig` warns if multiple configs are found, if excluded maps or locked action references do not exist, if configured binding groups are missing, or if a non-excluded action has an unsupported control type.
- Keep the config at the root of a `Resources` folder, not in `Resources/SomeFolder/`.
- Keep `Generated Folder` inside the project `Assets` directory. Do not point it at `Packages`, `Library`, or an absolute filesystem path.
- Wait for Unity to finish compiling after generating `GameActions.cs` before writing game code that references newly generated members.
- Action map and action names are converted to C# identifiers by removing characters other than letters, digits, and `_`; leading digits are prefixed with `_`, C# keywords get `_`, and collisions receive numeric suffixes. Prefer simple stable names to avoid relying on those suffixes.
- Generated action-map classes already define `Enable()` and `Disable()`. An action with either name is renamed in the generated facade. The outer generated class reserves `All`, `Register`, and `GameActions` for generated internals; maps with those names are suffixed to avoid invalid C#.
- Type mapping is exact and case-sensitive: action names ending in `Priority` become `PriorityAction`; otherwise `Vector2` becomes `Vector2Action`; `Axis` or `Analog` becomes `AnalogAction`; empty, `Button`, or `Digital` becomes `ButtonAction`. Other expected control types are skipped with a warning.
- The `Priority` suffix is case-sensitive and must be part of a longer name (for example `BackPriority`, not just `Priority`).
- Maps in `Excluded Maps` are omitted from `GameActions` and from the remapping map list. They are **not disabled** or removed from the runtime InputActionAsset; `Asset.Enable()` enables every map in the asset. This is useful for the Input System UI map.
- Do not use the frame-edge properties (`WasPressedThisFrame`, `WasReleasedThisFrame`, `StartedThisFrame`, `CanceledThisFrame`) in `FixedUpdate`. The wrapper clears these flags in `LateUpdate`. Read `IsPressed` or `Value` in physics code instead.
- The system only treats `Keyboard`, `Mouse`, and `Gamepad` activity as active-device changes. Touch and other joystick devices do not switch the active-device state.
- Remapping is immediately applied to the runtime action but is **not saved to disk** until `GameInputBindings.SaveMap(mapName)` is called.

## Editor automation API

The package includes public, Editor-only methods so an agent does not need to click menu items, drive custom inspectors or edit `.inputactions` JSON. They are in `Arosoul.GameInput.EditorTools.GameInputAgentTools` and compile into `Arosoul.GameInput.Editor`.

```csharp
using Arosoul.GameInput;
using Arosoul.GameInput.EditorTools;
using UnityEditor;
using UnityEngine.InputSystem;

var config = GameInputAgentTools.CreateConfig();
var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/Game.inputactions");
GameInputAgentTools.ConfigureConfig(
    config,
    actions,
    generatedFolder: "Assets/Generated",
    excludedMaps: new[] { "UI" },
    lockedActions: new[] { "Player/Pause", "Player/Look" },
    keyboardScheme: "Keyboard&Mouse",
    gamepadScheme: "Gamepad",
    conflictMode: RebindConflictMode.Swap,
    autoGenerate: true);

string[] diagnostics = GameInputAgentTools.ValidateConfig(config);
// Fix every diagnostic beginning with "ERROR:". Review WARNING entries.

string generatedPath = GameInputAgentTools.GenerateActions(config);
```

Available methods:

| Method | Use |
|---|---|
| `FindConfig()` | Find the first `GameInputConfig` asset. The package expects one. |
| `CreateConfig(path = "Assets/Resources/GameInputConfig.asset")` | Create the config if none exists; otherwise return/select the existing one. The supplied path must end in `/Resources/GameInputConfig.asset`. |
| `MoveConfigToResources(config, resourcesFolder = "Assets/Resources")` | Move an existing config into a folder named `Resources` and return the reloaded asset. Use it when validation reports an invalid location. |
| `ConfigureConfig(...)` | The required `actions` argument is always assigned (`null` clears it). Optional arguments cover the generated folder, excluded/locked maps/actions, device behavior, control-scheme names, cancel controls, conflict mode, auto-generation, and logging. Pass `null` for optional arguments to leave those fields unchanged; pass an empty list to clear exclusions or locks. |
| `ValidateConfig(config = null)` | Return `ERROR:` and `WARNING:` diagnostics before generation. |
| `GenerateActions(config = null)` | Validate, generate, import, and return the `GameActions.cs` asset path. |
| `AddAction(asset, map, action, type = Button, expectedControlType = null)` | Add an action (creating the map if needed). `expectedControlType` selects the generated wrapper: `null`/`Button` → `ButtonAction`, `Vector2` → `Vector2Action`, `Axis` → `AnalogAction`. Throws if the action exists. |
| `AddBinding(asset, "Map/Action", controlPath, groups = null)` | Add a binding such as `<Keyboard>/space`. Pass the config's keyboard or gamepad scheme name as `groups` so rebinding lists it under the right device. |
| `AddVector2Composite(asset, "Map/Action", up, down, left, right, groups = null)` | Add a 2D Vector composite (for example WASD). |
| `RemoveAction(asset, "Map/Action")` | Remove an action; returns `false` if it does not exist. |
| `SaveActions(asset)` | Write the edited asset to its `.inputactions` file and reimport. Returns the reimported asset; use it afterwards. The edit methods above only change memory until this is called. |
| `DescribeActions(config = null)` | Text summary: asset path, control schemes, maps (excluded marked), actions with their generated wrapper type and lock state, and bindings with groups. |
| `CreateConfigFromCommandLine()` | No-argument entry point for Unity `-executeMethod`. |
| `GenerateActionsFromCommandLine()` | No-argument generation entry point for Unity `-executeMethod` after the config is assigned. |

Example batch-mode calls:

```text
-executeMethod Arosoul.GameInput.EditorTools.GameInputAgentTools.CreateConfigFromCommandLine
-executeMethod Arosoul.GameInput.EditorTools.GameInputAgentTools.GenerateActionsFromCommandLine
```

To add or edit actions programmatically:

```csharp
var actions = GameInputAgentTools.FindConfig().Actions;
GameInputAgentTools.AddAction(actions, "Player", "Dash");
GameInputAgentTools.AddBinding(actions, "Player/Dash", "<Keyboard>/leftShift", "Keyboard&Mouse");
GameInputAgentTools.AddBinding(actions, "Player/Dash", "<Gamepad>/buttonEast", "Gamepad");
actions = GameInputAgentTools.SaveActions(actions);
GameInputAgentTools.GenerateActions(); // then wait for compilation before using GameActions.Player.Dash
```

For anything the helpers do not cover (interactions, processors, other composites), use the Unity Input System setup APIs (`InputActionSetupExtensions`) on the same asset and finish with `SaveActions`. Keep action map and action names stable because generated accessors are based on them. Add binding groups that match the config's keyboard/gamepad control scheme names if you want reliable per-device binding lists; without binding groups, the package falls back to classifying bindings by their control paths. When an asset is saved/imported, the package's postprocessor also queues generation if `Auto Generate` is enabled. Explicit generation is preferable in deterministic automation.

Unity menu equivalents:

- `Tools > Game Input > Create Config`
- `Tools > Game Input > Generate Actions`

## Runtime API

All entry points are static. Do not search for or create a `GameInput` singleton.

### Reading actions

```csharp
using Arosoul.GameInput; // GameInput, wrapper types. GameActions itself is global.

Vector2 move = GameActions.Player.Move.Value;
float look = GameActions.Player.Look.Value;
bool pressed = GameActions.Player.Jump.IsPressed;
bool downThisFrame = GameActions.Player.Jump.WasPressedThisFrame;
bool upThisFrame = GameActions.Player.Jump.WasReleasedThisFrame;

GameActions.Player.Jump.OnEvent += context => { /* started / performed / canceled */ };
```

- `ButtonAction`: `IsPressed`, `WasPressedThisFrame`, `WasReleasedThisFrame`, `OnEvent`.
- `Vector2Action`: `Value`, `X`, `Y`, `StartedThisFrame`, `CanceledThisFrame`, `OnEvent`.
- `AnalogAction`: `Value`, `StartedThisFrame`, `CanceledThisFrame`, `OnEvent`.
- `GameAction.InputAction` exposes the linked Unity `InputAction`; it is `null` before runtime initialization and outside a running initialized player.
- The edge flags remain available until `LateUpdate`; they are not fixed-timestep signals.

### Priority actions

Name an action `BackPriority`, `CancelPriority`, etc. to generate `PriorityAction`:

```csharp
var registration = GameActions.UI.BackPriority.Register(
    context => CloseTopmostMenu(), priority: 10);

registration.SetPriority(20);
GameActions.UI.BackPriority.Unregister(registration);
```

Only listeners at the highest current priority are invoked. If multiple listeners share that highest priority, all are invoked and a warning is logged. Keep the registration token and unregister it when the listener is no longer valid.

### Action maps

```csharp
if (GameInput.IsInitialized) {
    GameActions.Player.Disable();
    GameActions.Player.Enable();
}

GameInput.SetMapEnabled("Player", false);
bool enabled = GameInput.IsMapEnabled("Player");
```

The generated map methods delegate to `GameInput.SetMapEnabled`. A map can only be toggled after runtime initialization and if it exists in the configured asset.

### Device, cursor, and rumble

```csharp
GameInput.ActiveDevice;            // Keyboard or Gamepad (Keyboard includes mouse)
GameInput.ActiveGamepadKind;       // Xbox, PlayStation, Switch, Generic
GameInput.ActiveGamepad;
GameInput.OnActiveDeviceChanged += device => { };
GameInput.OnGamepadKindChanged += kind => { };

GameInput.SetMouseVisibilityPreference(true);
GameInput.Rumble(lowMotor, highMotor, durationSeconds);
GameInput.StopRumble();
GameInput.RumbleEnabled = false;
```

Device activity is detected from changed input controls using a magnitude threshold to avoid mouse jitter and stick drift. A disconnected active gamepad falls back to another connected pad or keyboard/mouse. Mouse visibility is a preference combined with the config's `Hide Mouse On Gamepad Use`; the API also changes `Cursor.lockState`.

Rumble motor values are clamped to `0..1`. Duration is measured using unscaled time. Rumble stops on timeout, when switching back to keyboard/mouse, when the application loses focus, or when explicitly stopped.

### Binding prompt labels

```csharp
string activePrompt = GameActions.Player.Jump.GetBindingLabel();
string keyboardPrompt = GameActions.Player.Jump.GetBindingLabel(GameInput.DeviceType.Keyboard);
```

`GetBindingLabel` chooses the first matching binding for the requested device. Composite bindings can be formatted as a combined label (for example `W/A/S/D`). Gamepad labels map common controls to brand-specific names (`A`, `Cross`, `B`, etc.); other paths use Unity's human-readable formatter. An empty string means no matching binding was found.

## Rebinding API

`GameInputBindings` operates on the runtime copy of the action asset.

```csharp
var entries = GameInputBindings.GetEntries("Player", GameInput.DeviceType.Keyboard);
var entry = entries[0];

string before = GameInputBindings.GetMapJson("Player");
bool started = GameInputBindings.StartRebind(entry, success => {
    if (success) GameInputBindings.SaveMap("Player");
});

GameInputBindings.CancelRebind();
GameInputBindings.ResetBinding(entry);
GameInputBindings.ResetMap("Player");
GameInputBindings.RestoreMapJson("Player", before);
GameInputBindings.SaveMap("Player");
```

- `GetMapNames()` lists non-excluded maps.
- `GetEntries(map, device)` returns one `RebindEntry` per remappable binding. A composite such as 2D Vector produces separate entries for its parts. `DisplayName` may include `#2`, `#3`, etc. for repeated bindings; `BaseName` is the group label.
- `StartRebind` returns `false` if runtime is not initialized or the entry is invalid. If it returns `true`, completion is asynchronous and the callback receives `true` for success or `false` for cancel/timeout. The operation has a 10-second timeout.
- The selected action is temporarily disabled while listening and restored to its previous enabled state afterward.
- Keyboard entries only accept keyboard/mouse controls; gamepad entries only accept gamepad controls. Pointer position/delta, mouse scroll, usage-based paths, and configured locked actions cannot be rebound.
- Cancel controls default to `Escape` for keyboard and `Start` for gamepad; both are configurable on `GameInputConfig`.
- `RebindConflictMode.Swap` (default) transfers the displaced binding's old path to the action that already used the newly selected path. `Allow` permits duplicate controls.
- `ResetBinding`, `ResetMap`, and `RestoreMapJson` change the runtime copy and raise `BindingsChanged`; call `SaveMap` to persist the result.
- `GetMapJson` / `RestoreMapJson` are useful for undo snapshots. The persistence file is `input-bindings.json` under `Application.persistentDataPath`.

The `Arosoul.PlayerSettings` package contains a ready-made rebinding section. Use it rather than writing a custom rebinding UI unless the game needs different presentation or behavior.

## Common workflows

### Add a player action

1. Add the action to the intended map: `GameInputAgentTools.AddAction` (or the Input Actions editor).
2. Set its expected control type deliberately (`Button`, `Vector2`, or `Axis`/`Analog`).
3. Add bindings and composites with `AddBinding`/`AddVector2Composite`, using the config's control scheme names as groups.
4. `SaveActions`, then `GenerateActions`.
5. Wait for compilation, then use `GameActions.<Map>.<Action>`.

### Suppress gameplay controls while a menu is open

Disable the gameplay map when opening the menu and re-enable it when closing. Do not disable the excluded `UI` map if the Input System UI module depends on it.

### Make only the topmost menu react to Back

Use one `BackPriority` action and register each open menu with a priority reflecting stack order. Unregister on close/destroy. Do not have every menu independently poll the same button.

### Build a settings rebinding page

Use the Player Settings `ActionMapSection` integration. Ensure the Game Input config exists in Resources and the integration assembly has compiled before adding its `input:<MapName>` entries to the settings layout.

## Troubleshooting and verification

- **`No GameInputConfig ... found`**: check asset name and Resources-root location; check the asset has an Input Actions reference.
- **`'<Map>/<Action>' no longer exists`**: the generated facade is stale or points at a different source asset; regenerate from the current config.
- **Action missing from generated class**: verify its map is not excluded and its expected control type is supported. Check C# identifier sanitization and collisions.
- **Bindings list is empty**: ensure the runtime initialized, the map name is exact, bindings have the configured control-scheme groups or device paths, and the action is not locked.
- **Rebind changes disappear after restart**: call `SaveMap(mapName)` after successful rebind/reset; the operation is not auto-persisted by the core API.
- **No gamepad prompt glyph**: check active device detection and `ActiveGamepadKind`; custom or unrecognized pads use generic names.

Before handing off, call `ValidateConfig`, generate the facade, confirm Unity compiles, and verify that the generated action members correspond to the current source asset.

## Source map

- `Runtime/GameInputConfig.cs`: config fields, excluded/locked map rules, device scheme names, conflict mode.
- `Runtime/GameInputRuntime.cs`: bootstrap, runtime asset clone, action linking, device event detection, persistent runner.
- `Runtime/GameInput.cs`: active device, mouse visibility, map enable/disable, rumble.
- `Runtime/Actions/`: action wrapper types and priority dispatch.
- `Runtime/Bindings/`: remapping, binding labels, gamepad glyphs.
- `Editor/GameActionsGenerator.cs`: typed facade generation and asset postprocessor.
- `Editor/GameInputAgentTools.cs`: public Editor automation API (config, validation, generation, action editing, description).
- `Editor/GameInputSetup.cs`: menu setup and `GAMEINPUT_INSTALLED` scripting define.

## Code conventions

This package uses its own established style; match it when editing these files: K&R braces (`namespace X {`, `void M() {`), private fields without underscore (`static bool queued;`), implicit `private`, and `#region` blocks at column 0. Keep runtime code free of `UnityEditor`. Public types and members get `/// <summary>` docs.
