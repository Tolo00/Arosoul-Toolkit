# SKILLS.md: Arosoul Commands

Guide for AI agents working with, extending, or writing code against `com.arosoul.commands`.

- **Package version:** 1.0.0
- **Unity version:** 6000.6 (Unity 6.6)
- **Language/UI:** C#, UI Toolkit (styled in code, no UXML/USS) for the in-game console and editor windows.
- **Assemblies/namespaces:** runtime `Arosoul.Commands`; editor `Arosoul.Commands.Editor`; sample `Arosoul.Commands.Samples`.
- **Dependency:** `com.unity.inputsystem` (keys are read from `Keyboard.current`). No dependency on other Arosoul packages.
- **Editor automation:** `Arosoul.Commands.Editor.CommandsAgentTools` (section 6). Prefer it over driving the windows.

---

## 1. Mental model

- A **command** is a method, property or field marked `[Command]`. `CommandInfo` describes it: `Name`, `Kind`
  (Method/Property/Field), `IsStatic`, `MonoTarget`, `Parameters`, `ValueType`, `CanRead`, `CanWrite`, `Signature`.
- **Discovery** (`CommandDiscovery`) runs once, on first use of `CommandRegistry.Default`. It scans every type in assemblies that
  reference `Arosoul.Commands` (plus Arosoul.Commands itself, for the built-ins). Assemblies referencing `nunit.framework` are skipped.
  Members with problems are not registered; they produce `CommandIssue`s (ERROR or WARNING).
- **Execution** (`CommandExecutor`) tokenizes input, finds commands by name (case-insensitive), parses arguments with `ValueParsers`,
  finds targets and writes results to a `CommandLog`. It never throws; failures are logged as errors.
- **Console** (`CommandConsole` MonoBehaviour) owns the UI (`CommandConsoleView`), the history and key handling, and mirrors
  `Application.logMessageReceived` into `CommandLog.Default`.

```text
input -> CommandTokenizer -> CommandRegistry.Find(name) -> overload/argument binding (ValueParsers)
      -> targets (static: null; instance: FindAnyObjectByType / FindObjectsByType, active only)
      -> invoke / get / set -> ValueFormatter -> CommandLog -> CommandConsoleView
```

## 2. Declaring commands

```csharp
[CommandPrefix("player.weapon")]                     // prefix for every command in the class
public class PlayerWeapon : MonoBehaviour
{
    [SerializeField, Command(MonoTargetType.All)]
    float _damage = 3f;                              // player.weapon.damage

    [Command("reload", "Refills the magazine.")]     // explicit name: player.weapon.reload
    public void Reload() { }

    [Command(Description = "Fires.", MonoTarget = MonoTargetType.First)]
    public int Fire(int shots = 1) => shots;         // player.weapon.fire [int shots = 1]
}
```

Rules:

- **Naming.** Without a name: kebab-case of the member name after stripping leading `_` and `m_`/`s_`/`k_`
  (`SetDamage` → `set-damage`, `FPSCounter` → `fps-counter`, `Vector3Value` → `vector3-value`). Explicit names are used as written.
  The class prefix (combined with prefixes of outer classes for nested types) is always prepended with a dot.
  Valid names contain only letters, digits, `.`, `-`, `_`.
- **Static vs instance.** Static members need no target. Instance members must be declared on a `Component` type; otherwise ERROR.
  `MonoTargetType.First` (default) runs on one active instance (`FindAnyObjectByType`, order not defined); `All` runs on every
  active instance. Inactive objects are never targeted. No instance → a WARNING line in the console and the command does not run.
- **Members.** Public and non-public, static and instance. `BindingFlags.DeclaredOnly`, so an inherited `[Command]` member is registered once,
  on its declaring base class, with the base class prefix; instance lookup by the base type also finds derived components.
- **Properties/fields.** Read with `name`, write with `name = value` or `name value`. Read-only properties, `readonly` and `const`
  fields are read-only. A settable member of an unsupported type is registered read-only with a WARNING.
- **Methods.** Parameters must be supported types, passed by value (no `ref`/`in`/`out`/generic methods). Optional parameters may be
  omitted. Overloads are tried by parameter count, then by whether the arguments parse. If there are more arguments than parameters and
  the last parameter is a `string`, the remaining words are joined into it (`echo hello world`). Non-void results are printed.
- **Conflicts.** Two values with the same name → ERROR (the first wins). A value and a method with the same name → ERROR (the value wins).
  Overloads with identical parameter types → WARNING.
- `[Command]` derives from `UnityEngine.Scripting.PreserveAttribute` to resist stripping.

## 3. Values and syntax

| Type | Accepted text |
|---|---|
| `string` | anything; `"quoted text"` for spaces; `\"` escapes |
| `bool` | `true/false/1/0/yes/no/on/off` |
| numbers | invariant culture (`1.5`, not `1,5`); a trailing `f` is allowed |
| enums | name (case-insensitive, `-`/`_` ignored), number, flags `A|B` or `A,B` |
| `Vector2/3/4`, `Vector2Int/3Int` | `(1, 2, 3)`, `1,2,3`, `[1 2 3]`; after `=` also `1 2 3` |
| `Quaternion` | Euler `(x, y, z)` or `(x, y, z, w)` |
| `Color`, `Color32` | `#RRGGBB`, `#RRGGBBAA`, bare hex `FF8800`, or `ColorUtility` names (`red`, `orange`, `cyan`…) |

Output (`ValueFormatter`) uses the same syntax: vectors `(1, 2.5, 0)`, colors `#RRGGBBAA`, collections `[a, b]`, objects by name.

Custom types: subclass `ValueParser<T>` (non-abstract, parameterless constructor). It is found by the same assembly scan and overrides
built-in parsing for `T`. Register manually with `ValueParsers.Register` (for example in tests, where assemblies are not scanned).

## 4. Runtime API

```csharp
CommandAPI.Execute("player.weapon.damage = 5");   // returns false on error; output goes to CommandAPI.Output
CommandAPI.Log("text"); CommandAPI.LogWarning("..."); CommandAPI.LogError("...");
CommandAPI.Registry.Find("help");                 // CommandRegistry.Default
CommandAPI.UnityLogLevel = UnityLogLevel.Log;     // also the log-level command
CommandConsole.Instance?.Open();                  // Close, Toggle, IsOpen, OnOpened, OnClosed, History, Execute (adds to history)
```

For isolated use (tests, tools): `new CommandExecutor(new CommandRegistry(new[] { typeof(MyCommands) }), new CommandLog())`.

Built-in commands (`Runtime/BuiltIn/BuiltInCommands.cs`): `help [command]`, `commands [filter]`, `clear`, `echo <text>`,
`time-scale`, `target-frame-rate`, `fps` (read-only), `log-level`, `quit`.

## 5. Console, settings and builds

- **Setup:** the `Prefabs/Commands Console.prefab` (just a `CommandConsole`) goes in the first scene; **GameObject > Arosoul > Commands Console**
  or `CommandsAgentTools.AddConsoleToActiveScene()` adds it. With `DontDestroyOnLoad` (default) it is moved to the root and persists.
  Later consoles destroy themselves.
- **Settings:** `CommandsSettingsData` in `ProjectSettings/CommandsSettings.asset` (`CommandsSettings` ScriptableSingleton), edited in
  **Tools > Arosoul Commands > Settings**. Baked by `CommandsSettingsBaker` to `Assets/Generated/Commands/CommandsRuntimeSettings.asset`
  and added to Player Settings' preloaded assets. Baking runs on every settings change, before every build, and once on editor load
  if the asset is missing. **Do not hand-edit either asset**; use the window or `CommandsAgentTools.ModifySettings`.
- **Availability:** `EditorOnly`, `DevelopmentBuilds` (default) or `AllBuilds`. Always available in the editor. Elsewhere `CommandConsole`
  destroys itself in `Awake`, so the registry is never built.
- **Keys** (Input System `Key`): toggle `` ` ``, close Esc, submit Enter (Numpad Enter too), autocomplete Tab, previous/next Up/Down.
  Up/Down select suggestions when selectable suggestions are shown, otherwise browse history. While a console key is held,
  the input field ignores the key (no focus change, caret move or typed character).
- **Suggestions:** while typing the name, `CommandAutocomplete` ranks exact > prefix > segment start (after `.`, `-`, `_`) > substring > fuzzy.
  Tab fills the name (plus a space for methods with parameters). After the name (space or `=`), the matching overloads are shown as
  non-selectable usage hints.
- **Rendering:** the console creates a child `UIDocument` with its own `PanelSettings` (ScaleWithScreenSize 1920x1080, sorting order 32100,
  above the Transitions overlay) unless `PanelSettings` is set. Without a `Theme`, text does not render in builds; the baker fills it with
  `UnityDefaultRuntimeTheme` when empty.

## 6. Editor automation API (`CommandsAgentTools`)

| Method | Purpose |
|---|---|
| `Validate()` | `ERROR:`/`WARNING:` strings: discovery issues, runtime settings missing/out of sync/not preloaded, key conflicts, missing theme, missing prefab. Empty = valid. |
| `Describe()` | Settings plus every command grouped by declaring class (signature, kind, target, member, description) and issues. |
| `DescribeSettings()` | Settings only. |
| `FindCommands(query, max)` | Commands ranked like autocomplete. |
| `ModifySettings(Action<CommandsSettingsData>)` | Change, save and bake settings, e.g. `s => s.ToggleKey = Key.F1`. |
| `Bake()` | Bake runtime settings; returns the asset path. |
| `Execute(input)` | Run input in edit or play mode; returns output lines (`WARNING: `/`ERROR: ` prefixed). Instance commands need loaded scenes (play mode for runtime objects). |
| `AddConsoleToActiveScene()` | Instantiate the prefab (or return the existing console). Does not save the scene. |

Typical agent loop after adding commands: recompile → `CommandsAgentTools.Validate()` → fix errors → `CommandsAgentTools.Execute("...")`
(enter play mode first for instance commands) → check output.

Other editor entry points: `CommandsDebuggerWindow` (Tools > Arosoul Commands > Debugger), `CommandSourceLocator.TryFind/TryOpen(member)`.

## 7. Rules for agents and gotchas

- Do not add `[Command]` to members of plain (non-Component) classes unless they are static.
- Name collisions are case-insensitive. Check `Validate()` after adding commands.
- Inactive objects and objects in unloaded scenes are not targeted.
- `CommandRegistry.Default` is built once per domain; it reflects code after a recompile, not commands added at runtime.
- Test assemblies are never scanned. Tests must build their own `CommandRegistry` and register custom parsers manually.
- Commands run on the main thread inside `Update`; long work should start a coroutine/Awaitable and log when done.
- Do not call `Debug.Log` from code that handles `CommandLog` events, or Unity log mirroring will re-enter the log.

## 8. Extending and testing

- New built-ins go in `BuiltInCommands` (static, private is fine).
- New value types: add to `ValueParsers.CanParse`/`TryParse` and `ValueFormatter.Format`, with tests in `CommandParsingTests`.
- Edit mode tests (`Tests/Editor`): naming, tokenizer, parsers, formatter, discovery issues, execution, autocomplete, history, baker, agent tools, source locator.
  `TestCommands`/`InvalidCommands` hold the fixtures.
- Play mode tests (`Tests/PlayMode`): `First`/`All`/no-instance targeting with `TestTarget`, console open/close/duplicates, pause, log mirroring.

## 9. File map

| Path | Contents |
|---|---|
| `Runtime/Attributes/` | `CommandAttribute`, `CommandPrefixAttribute`, `MonoTargetType` |
| `Runtime/Core/` | `CommandInfo`, `CommandKind`, `CommandIssue`, `CommandDiscovery`, `CommandAssemblyScanner`, `CommandRegistry`, `CommandExecutor`, `CommandAutocomplete`, `CommandHistory`, `CommandLog*`, `CommandAPI` |
| `Runtime/Parsing/` | `CommandTokenizer`, `ValueParsers`, `ValueParser<T>`, `IValueParser`, `ValueFormatter`, `TypeNames` |
| `Runtime/Console/` | `CommandConsole`, `CommandConsoleView`, `CommandsSettingsData`, `CommandsRuntimeSettings`, `CommandsAvailability`, `UnityLogLevel` |
| `Runtime/BuiltIn/` | `BuiltInCommands` |
| `Editor/` | `CommandsSettings`, `CommandsSettingsBaker` (+ build preprocessor, bootstrap), `CommandsAgentTools`, `CommandsConsoleCreator`, `CommandSourceLocator` |
| `Editor/Windows/` | `CommandsSettingsWindow`, `CommandsDebuggerWindow` |
| `Prefabs/` | `Commands Console.prefab` |
| `Samples/` | `CommandsSamplePlayer` |

## 10. Code conventions

Allman braces, 4 spaces. Private fields `_camelCase`; `[SerializeField]` fields omit `private`. Public fields only on `[Serializable]`
data classes (`CommandsSettingsData`). Events `On…` invoked with `?.Invoke`. Explicit access modifiers on Unity messages.
`#region`s in larger classes. Tooltips as full sentences on serialized fields.
