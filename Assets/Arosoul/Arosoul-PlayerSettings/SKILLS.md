# Skill: Arosoul Player Settings

## Purpose and boundaries

A runtime player-settings system built with Unity UI Toolkit. It separates setting definitions and values from menu layout and presentation:

1. **Settings catalog**: built-in `SettingDefinition` classes plus generated custom value settings, optional setting sections, and layout decorations.
2. **Layout asset**: a `SettingsLayout` ScriptableObject containing tabs/categories, ordered entries, apply mode, and optional theme.
3. **Runtime UI**: `SettingsMenu` hosts a `SettingsMenuView` on a `PanelRenderer` and creates UI from the layout and UXML templates.

Runtime namespace and assembly: `Arosoul.PlayerSettings` / `Arosoul.PlayerSettings`  
Editor automation namespace: `Arosoul.PlayerSettings.EditorTools`  
Editor assembly: `Arosoul.PlayerSettings.Editor`

The core system does not depend on the Arosoul GameInput package (`Arosoul.GameInput`). The optional Game Input integration lives under `Integrations/GameInput`. The supplied core asmdef explicitly references `Unity.InputSystem`; ensure that package is installed in projects using this package. URP brightness is a separate optional integration.

The package can be imported as folders under `Assets/`; a UPM manifest is not required. Preserve the `Runtime`, `Editor`, `Integrations`, `Resources`, and `.asmdef` structure.

## Architecture and data flow

- `SettingDefinition` describes a value: stable id, label, type, default, optional range/options, display formatting, availability, apply order, and `Apply` behavior.
- `SettingsRegistry` discovers setting/display/section/decorations types and builds the catalog.
- `SettingsStore` owns **applied** and **pending** values. The default UI stages edits for manual categories and applies immediately for instant categories.
- `GameSettings` is the static runtime facade. The generated `CustomSettings` class supplies typed accessors for user-defined value settings.
- `SettingsLayout` determines which definitions or sections are shown, which display is selected, and their order within each category.
- `SettingsMenuView` instantiates the default or themed UXML, binds values, handles category-scoped Apply/Undo/Reset, and manages the unsaved-changes prompt.

Settings values persist in `player-settings.json` under `Application.persistentDataPath` by default. The `ISettingsStorage` interface allows a different backend.

## Agent rules

- **Do not hand-edit generated `CustomSettings.cs`.** Author custom values through `PlayerSettingsAgentTools.GenerateCustomSettings` or the Custom Settings EditorWindow.
- Treat a setting id as a persistence key. Renaming a custom setting's API name creates a new key and the old saved value will no longer be used.
- Do not edit the UXML templates to add a setting value. Add a setting definition and a layout entry; use a custom display only when the built-in displays are insufficient.
- Do not assume values returned by `CustomSettings.X` include uncommitted menu edits. The generated property reads the **applied** value. Use `GameSettings.Store.GetCurrent(id)` to inspect a pending value.
- A direct `GameSettings.Set(...)` applies immediately. Use `GameSettings.Store.SetPending(...)` only when intentionally staging an edit from code.
- In a manual category, player changes remain pending until Apply. Undo/Discard restores the applied values. Reset stages defaults and requires Apply. In an instant category, changes and Reset apply immediately.
- Apply, Undo, and Reset in the menu operate on the selected category only. `GameSettings.Apply()`, `Revert()`, and `ResetToDefaults()` are global facade operations.
- Keep `SettingDefinition` constructors free of Unity API calls. Definitions are instantiated in Edit Mode for the layout editor as well as at runtime.
- `SettingDisplay` instances are shared by all settings rows. Keep them stateless; put row-specific data in locals captured by callbacks.
- Custom setting `apiName` values and enum option ids must be valid C# identifiers. API names also cannot use reserved generated names, collide with built-in ids, duplicate another API name, or collide with a generated enum type. Enum options are ids, not display labels. Use ids such as `Low`, `Medium`, `High`; their labels are nicified by default.
- Custom `Int` and `Float` settings require `min < max`; values are clamped to that range. Float defaults and serialized numbers use invariant culture.
- If a custom `SettingDefinition`, `SettingDisplay`, `SettingSectionProvider`, or `LayoutDecoration` subclass lives in another assembly, that assembly must reference `Arosoul.PlayerSettings`. Preserve that assembly in `link.xml` for IL2CPP stripping.
- Avoid namespace names that conflict with Unity namespaces or types (for example, do not name a namespace `Editor`).

## Custom settings authoring

Custom settings are value definitions only; they do not have a custom `Apply` method. Other systems read the value and apply it to game-specific systems (for example, route a float setting to an AudioMixer yourself).

Supported types:

| `SettingType` | Generated C# property | Required authoring data |
|---|---|---|
| `String` | `string` | Default string (optional) |
| `Int` | `int` | `min`, `max`, optional default |
| `Float` | `float` | `min`, `max`, optional default |
| `Bool` | `bool` | `true` or `false` default |
| `Enum` | `<ApiName>Option` enum | At least one unique identifier option; default must match an option id |

Generated code example:

```csharp
float sensitivity = CustomSettings.MouseSensitivity;
CustomSettings.MouseSensitivity = 0.7f; // calls GameSettings.Set; applies immediately

CustomSettings.AimAssist = AimAssistOption.Medium;
```

### Editor automation API

The package adds public Editor-only authoring APIs in `Arosoul.PlayerSettings.EditorTools.PlayerSettingsAgentTools`. They can be called from an Editor script, a Unity `-executeMethod` entry point, or an automation bridge without opening the custom windows.

```csharp
using Arosoul.PlayerSettings;
using Arosoul.PlayerSettings.EditorTools;

var rows = new[] {
    new CustomSettingData {
        apiName = "MouseSensitivity",
        displayName = "Mouse Sensitivity",
        type = SettingType.Float,
        min = 0.1f,
        max = 2f,
        defaultValue = "0.7"
    },
    new CustomSettingData {
        apiName = "AimAssist",
        displayName = "Aim Assist",
        type = SettingType.Enum,
        options = new[] { "Off", "Low", "Medium", "High" },
        defaultValue = "Medium"
    }
};

string[] errors = PlayerSettingsAgentTools.ValidateCustomSettings(rows);
// Fix all returned validation errors before proceeding.
string scriptPath = PlayerSettingsAgentTools.GenerateCustomSettings(rows); // replaces the whole list

// To add or change a single setting without touching the others:
PlayerSettingsAgentTools.AddOrUpdateCustomSetting(new CustomSettingData {
    apiName = "ShowHints", displayName = "Show Hints", type = SettingType.Bool, defaultValue = "true"
});
```

Available methods:

| Method | Use |
|---|---|
| `LoadCustomSettings(out scriptPath)` | Read the current custom settings back as editable row data. Returns what was last written if Unity has not recompiled since, otherwise the compiled generated class. |
| `AddOrUpdateCustomSetting(row)` | Add one setting, or replace the one with the same `apiName`, keeping all others and the script location. Validates and regenerates. Safe to call repeatedly before Unity recompiles. |
| `RemoveCustomSetting(apiName)` | Remove one setting and regenerate; returns `false` if it does not exist. Remove its layout entries as well. |
| `ValidateCustomSettings(rows)` | Validate ids, generated enum names, types, defaults, ranges, and collisions with built-in ids. |
| `GenerateCustomSettings(rows, outputFolder = "Assets/Settings")` | Generate/import `CustomSettings.cs`; returns its asset path. Output folder must be inside `Assets`. |
| `GetAvailableItemIds()` | List registered setting, section, and decoration ids for layout authoring. |
| `GetAvailableDisplayIds(itemId)` | List supported display ids for a setting or section; decorations have no display id. |
| `CreateLayout(path, title, orientation, showCloseButton, theme)` | Create a `SettingsLayout` asset. The path must be under `Assets` and end in `.asset`. |
| `ConfigureLayout(layout, title, orientation, showCloseButton, theme)` | Update the layout's top-level properties. |
| `AddCategory(layout, name, applyMode, showResetButton)` | Add and return a category. |
| `FindCategoryIndex(layout, name)` | Index of the first category with that name, or `-1`. Use it to get the `categoryIndex` the other methods take. |
| `DescribeLayout(layout)` | Text summary of the layout: properties, then each `[index]` category with mode/reset and its `[index]` entries, display ids and parameters. |
| `ConfigureCategory(layout, categoryIndex, name, applyMode, showResetButton)` | Update a category by its current ordered index. |
| `MoveCategory(layout, fromIndex, toIndex)` / `RemoveCategory(layout, categoryIndex)` | Reorder or remove categories. Indices are current zero-based list positions. |
| `AddEntry(layout, categoryIndex, itemId, displayType, parameters)` | Add a setting, section, or decoration. Repeated entries are allowed. If `displayType` is omitted, the first/default supported display is selected. |
| `ConfigureEntry(layout, categoryIndex, entryIndex, itemId, displayType, parameters)` | Replace an existing entry after validating its id, display and decoration parameters. |
| `MoveEntry(layout, categoryIndex, fromIndex, toIndex)` / `RemoveEntry(layout, categoryIndex, entryIndex)` | Reorder or remove entries. Indices are current zero-based list positions. |
| `ValidateLayout(layout)` | Check ids, display compatibility, platform availability, and decoration parameters. Each diagnostic starts with `ERROR:` or `WARNING:` and names the category, entry index and item id. |

**Important sequencing:** after `GenerateCustomSettings`, Unity must compile the new C# file before the new setting can be discovered by `SettingsRegistry` and added to a layout. Run layout authoring after compilation/domain reload, then call `SettingsRegistry.Rebuild()` or `PlayerSettingsAgentTools.ValidateLayout(layout)` to refresh and verify the catalog.

Example layout authoring after custom settings have compiled:

```csharp
var layout = PlayerSettingsAgentTools.CreateLayout("Assets/Settings/MainSettings.asset", "Settings");
PlayerSettingsAgentTools.AddCategory(layout, "Graphics", ApplyMode.Manual);
int graphicsIndex = PlayerSettingsAgentTools.FindCategoryIndex(layout, "Graphics");
PlayerSettingsAgentTools.AddEntry(layout, graphicsIndex, "resolution");
PlayerSettingsAgentTools.AddEntry(layout, graphicsIndex, "quality_level");
PlayerSettingsAgentTools.AddEntry(layout, graphicsIndex, "vsync");
PlayerSettingsAgentTools.AddEntry(layout, graphicsIndex, "MouseSensitivity");
string[] diagnostics = PlayerSettingsAgentTools.ValidateLayout(layout);
```

The APIs are Editor-only and must run on Unity's main Editor thread. For batch mode, place a parameterless `public static` wrapper method in an Editor assembly; construct the input data there and call the relevant API. Unity's `-executeMethod` cannot directly provide arguments to these methods.


The EditorWindow equivalents are:

- `Tools > Player Settings > Custom Settings`
- Create a layout through `Create > Player Settings > Settings Layout`
- Create a theme through `Create > Player Settings > Settings Theme`
- Select a `SettingsLayout` asset and use **Edit Categories** in its inspector.

## Layout authoring

`SettingsLayout.Categories` is an ordered mutable list. Each `CategoryLayout` contains:

- `name`: tab label.
- `applyMode`: `ApplyMode.Manual` or `ApplyMode.Instant`.
- `showResetButton`: whether the category footer shows Reset.
- `entries`: ordered `EntryLayout` records.

Each `EntryLayout` has a `settingId`, a `displayType`, and optional `parameters` (used by decorations). A setting can appear more than once in the same or different categories. A `SettingSection` can also appear more than once with different display ids; this is how keyboard and gamepad keybind views are placed separately.

Built-in setting ids in this package:

| Id | Type | Behavior |
|---|---|---|
| `resolution` | Enum | Distinct screen sizes, stored as `widthxheight`; defaults to the current size when available and applies through `Screen.SetResolution`. |
| `display_mode` | Enum | `Fullscreen`, `Borderless`, `Windowed`; defaults to `Borderless` and combines with resolution through `WindowState`. |
| `frame_rate_limit` | Enum | 30, 60, 120, 144, 240, or `unlimited`; defaults to `unlimited` and changes `Application.targetFrameRate`. |
| `quality_level` | Enum | Uses project Quality settings names and the current quality level as default; applies before other settings (`Order = -100`). |
| `vsync` | Bool | Defaults to `true`; changes `QualitySettings.vSyncCount`. |
| `master_volume` | Float | 0..1, default 0.8; changes `AudioListener.volume`. |
| `brightness` | Float | 0..1, default 0.5; only registered when the URP integration is available. |

Default display ids:

| Value type | Display ids |
|---|---|
| `Bool` | `toggle` |
| `Int` / `Float` | `slider` (default), `stepper` |
| `Enum` | `dropdown` (default), `option-stepper` |
| `String` | `text` |

`PlayerSettingsAgentTools.GetAvailableDisplayIds(itemId)` is authoritative for any custom or integration-provided type. The runtime falls back to the default supported display if a setting entry stores an unknown or incompatible display id; validation warns about this.

### Layout decorations

Decorations are UI-only entries, not values. Built-in ids:

- `decoration:header` — `text` parameter, default `Header`.
- `decoration:separator` — no parameters.
- `decoration:spacing` — numeric `height` parameter, default `16`.

Use invariant-culture numeric strings in decoration parameters (for example `"12.5"`).

### Themes and UXML

A `SettingsTheme` can override menu, tab, category, footer-button, prompt, and per-display UXML templates, and add style sheets. Empty overrides fall back to `Runtime/Resources/PlayerSettings`. `Include Default Style` controls whether the default USS is included; custom USS files are then added.

Default display templates expect these element names:

- `Display_slider.uxml`: required `Slider` named `slider`; optional `Label` named `label` and `value`.
- `Display_stepper.uxml`: required `Button`s named `decrease` and `increase`; optional `Label`s named `label` and `value`.
- `Display_toggle.uxml`: required `Toggle` named `toggle`; optional `Label`s named `label` and `value`.
- `Display_dropdown.uxml`: required `DropdownField` named `dropdown`; optional `Label` named `label`.
- `Display_text.uxml`: required `TextField` named `field`; optional `Label` named `label`.

Read the comments in each default UXML before replacing a template. The menu and structural templates also have required named elements; preserve those names when overriding them. Keep XML comments valid: do not include `--` inside a comment.

## Runtime value API

```csharp
using Arosoul.PlayerSettings;

float volume = GameSettings.GetFloat("master_volume");
bool vsync = GameSettings.GetBool("vsync");
GameSettings.Set("vsync", false); // immediate apply + queued persistence

GameSettings.Changed += id => { /* applied value changed */ };
GameSettings.Applied += () => { /* batch applied */ };

GameSettings.HasPending;
GameSettings.Apply();
GameSettings.Revert();
GameSettings.ResetToDefaults(applyImmediately: true);
```

- `GameSettings.Get<T>` and the typed helpers return **applied** values.
- `GameSettings.Set(id, value)` validates/coerces the value, applies it immediately, raises `Changed` if it changed, and requests persistence.
- `GameSettings.Store.GetCurrent(id)` returns the pending value if one exists, otherwise the applied value. `GetApplied`, `IsPending`, and `TryGetDefinition` are also available on the store.
- `GameSettings.Store.SetPending(id, value)` stages a value; `ApplyPending(ids)` commits selected ids, `RevertPending(ids)` discards pending values, and `ResetToDefaults(ids, applyImmediately)` can be scoped to ids.
- The `CustomSettings` generated properties use `GameSettings.Get<T>` and `GameSettings.Set`, so their setters apply immediately and their getters do not expose pending UI edits.
- Startup initializes the registry and store, loads persisted values (or defaults), applies them, and saves after a short debounce and on application pause/quit.
- To use custom persistence, implement `ISettingsStorage` and call `GameSettings.Initialize(storage)`. This rebuilds the registry, replaces the store, loads, and applies values. Do this before other gameplay systems rely on settings.

## Runtime menu integration

1. Add a `PanelRenderer` to a GameObject.
2. Assign a `Panel Settings` asset to the `PanelRenderer`; leave its `Source Asset` empty so the system can add its generated visual tree.
3. Add `SettingsMenu` to the same GameObject and assign the `SettingsLayout` asset.
4. Call `Open()`, `Close()`, or `Toggle()` on `SettingsMenu`.

`SettingsMenu` also exposes `IsOpen` and a `Closed` event. Its serialized `visibleOnStart` defaults to `true`.

### Apply, undo, reset, and prompts

- Apply Mode and Reset visibility are configured per category, not globally.
- In **Manual** mode, edits are pending. Apply commits that category; Undo discards that category's pending changes; Reset stages that category's defaults and still requires Apply.
- In **Instant** mode, each edit and Reset are applied immediately.
- Switching tabs with pending changes in the current category asks whether to apply or discard that category. Closing with any pending changes asks about all categories/sections.
- Calling `Close()` again while the unsaved-changes prompt is open dismisses only the prompt. The menu stays open and changes remain untouched.
- The menu's Apply/Undo/Reset footer operates on the selected category. Do not replace these with global `GameSettings.Apply()` calls unless a global action is intended.

### Navigation

The menu supports pointer and UI Toolkit navigation. It tracks focus across tabs, rows, footer controls, dropdown overlays, and the save/discard prompt. Controller navigation is driven through UI Toolkit navigation events; the package also has optional raw Input System shoulder-button handling for tab changes when its Input System compile define is active. Rebinding capture blocks menu navigation briefly so the captured key/button is not also treated as a UI action.

## Extending the system

### Add a built-in setting with apply logic

Create a public, concrete `SettingDefinition` subclass with a public parameterless constructor. Implement `Id`, `DisplayName`, `Type`, `DefaultValue`, and `Apply`; override `Group`, `Order`, `IsAvailable`, `Min`, `Max`, `Step`, `Options`, `GetOptionLabel`, or `FormatValue` when needed. It is discovered automatically. Keep the constructor free of Unity API calls; use Unity APIs from `DefaultValue`/`Apply` as appropriate.

### Add a display type

Create a public, concrete `SettingDisplay` subclass with a public parameterless constructor. Implement `Id`, `DisplayName`, `Supports(type)`, and `Bind(root, binding)`. Add a `Display_<Id>.uxml` template under `Resources/PlayerSettings` or provide it through the theme. Lower `Priority` wins when choosing a default. Display instances are shared and must remain stateless.

### Add a non-value section

Create a `SettingSection` and a public, concrete `SettingSectionProvider` with a public parameterless constructor. The provider returns section instances. A section must have a unique id that does not collide with settings, other sections, or decorations. Implement `Build(SectionContext)` and, if it has staged state, `HasPending`, `ApplyPending`, `RevertPending`, `ResetToDefaults`, and `RaiseStateChanged` notifications. Each section display id is stored in the layout entry.

### Add a decoration

Create a public, concrete `LayoutDecoration` subclass with a public parameterless constructor. Implement `Build(DecorationContext)`. Decorations are UI-only and do not participate in Apply/Undo/Reset.

## Optional integrations

### Game Input (`Integrations/GameInput`)

The `Arosoul.PlayerSettings.GameInput` assembly references `Arosoul.GameInput` and is compiled when `GAMEINPUT_INSTALLED` is defined. Its code lives in the `Arosoul.PlayerSettings` namespace, where `GameInput` would resolve to the `Arosoul.GameInput` namespace; `ActionMapSection.cs` therefore declares `using GameInput = Arosoul.GameInput.GameInput;` inside its namespace block. Do the same in any new integration code. The Game Input package adds this define from its Editor setup. The integration discovers one section per non-excluded Input Action map and exposes these section ids:

- `input:<MapName>` with display id `keyboard` (`Keyboard Keybinds`).
- `input:<MapName>` with display id `gamepad` (`Gamepad Keybinds`).

Add the same section id twice to a category if both device views are required, choosing a different display id for each entry. Rebinding changes the runtime Input Actions copy immediately but is committed to disk only when the category is applied. Undo/discard restores the map snapshot. The Game Input `GameInputConfig` must be available through `Resources.Load` for the provider to discover maps.

Templates and stylesheet: `Rebind_Section.uxml`, `Rebind_Row.uxml`, and `RebindDefault.uss` under `Integrations/GameInput/Resources/PlayerSettings`. Delete the integration folder/assembly if the project does not use Game Input.

### URP brightness (`Integrations/URP`)

The `Arosoul.PlayerSettings.URP` assembly is enabled by the `com.unity.render-pipelines.universal` version define. It contributes `brightness`, implemented through a persistent global Volume and `ColorAdjustments.postExposure` mapped from `-2` to `+2` stops. It is not available in projects without URP.

## Platform and build notes

- Resolution and display-mode changes use `Screen.SetResolution`; they do not behave like a real player build when inspected in the Editor Game view.
- `quality_level` is applied before other settings because changing quality can reset VSync state.
- Brightness needs URP and its runtime assemblies.
- Add external assemblies containing reflected subclasses to `link.xml` (or equivalent linker configuration) for IL2CPP. The supplied file preserves the core, URP, and Game Input integration assemblies.
- The core asmdef includes a direct `Unity.InputSystem` assembly reference. The package is independent of the Arosoul Game Input wrapper, but the Unity Input System package itself should be present.

## Troubleshooting and verification

- **Setting missing from layout editor**: run `SettingsRegistry.Rebuild()`. For custom settings, first ensure the generated script compiled and the `[GeneratedSettings]` class exposes its `Data` array.
- **Custom setting not found by `AddEntry`**: do not add it in the same step as code generation. Wait for Unity's compile/domain reload, then refresh the registry and try again.
- **Layout entry silently absent at runtime**: check `ValidateLayout`; verify the item id exists and `IsAvailable` is true on the target platform.
- **Unexpected display**: check that the entry's `displayType` supports its setting type; the runtime falls back to the default display when it does not.
- **Changes seem lost**: custom properties expose applied values; inspect `GameSettings.Store.GetCurrent(id)` for pending edits. For manual categories, Apply is required.
- **Game Input keybind section missing**: verify the Game Input package, `GAMEINPUT_INSTALLED` define, `GameInputConfig` in Resources, and the action-map entry id/display id.
- **URP brightness missing**: verify URP is installed and the integration asmdef's version define is active.

Run the `Arosoul.PlayerSettings.Tests.Editor` test assembly (custom setting validation and layout authoring) after changing the editor tooling.

Before handoff, run `PlayerSettingsAgentTools.ValidateCustomSettings` (if generating custom settings), wait for compilation, run `ValidateLayout`, and verify in Play Mode that Apply/Undo/Reset and the intended navigation work.

## Source map

- `Runtime/Core/`: definitions, registry, values, store, persistence interface, and `GameSettings` facade.
- `Runtime/Layout/`: `SettingsLayout`, `SettingsTheme`, and layout decorations.
- `Runtime/UI/`: menu component/view, navigation, setting bindings, and display implementations.
- `Runtime/BuiltIn/`: built-in window, graphics, and audio settings.
- `Runtime/Resources/PlayerSettings/`: default UXML and USS templates.
- `Editor/CustomSettingsWindow.cs` and `CustomSettingsCodeGenerator.cs`: typed custom setting authoring and generation.
- `Editor/SettingsLayoutWindow.cs`: category and entry authoring UI.
- `Editor/PlayerSettingsAgentTools.cs`: public Editor automation API for agents and scripts.
- `Integrations/GameInput/`: input rebinding section.
- `Integrations/URP/`: URP brightness setting.
- `Tests/Editor/`: editor tests for custom setting validation and the layout automation API.
- `link.xml`: linker preservation for reflection-discovered types.

## Code conventions

This package uses its own established style; match it when editing these files: K&R braces (`namespace X {`, `void M() {`), private fields without underscore, implicit `private`, `#region` blocks at column 0, and `/// <summary>` on public members. Keep runtime code free of `UnityEditor`. Every writer of `CustomSettings.cs` must go through `CustomSettingsCodeGenerator.Write` so `LoadCustomSettings` stays correct before recompilation.
