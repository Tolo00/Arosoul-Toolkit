# Arosoul Commands

In-game command console for Unity 6.6, rendered with UI Toolkit. Expose methods, properties and fields with `[Command]`
and use them from a drop-down console with autocomplete, history and mirrored Unity logs.

## Setup

1. Add the console to your first scene: **GameObject > Arosoul > Commands Console** (or the button in the settings window).
   It survives scene loads by default; a second console destroys itself.
2. Adjust settings in **Tools > Arosoul > Commands > Settings**: availability (Editor only, Development builds, All builds),
   keys, pause while open, Unity log level, history, size and font.

Settings are stored in `ProjectSettings/CommandsSettings.asset` and baked into `Assets/Generated/Commands/CommandsRuntimeSettings.asset`
(a preloaded asset). Baking happens on every change and before builds. Where the console is not available it destroys itself on Awake.

## Defining commands

```csharp
using Arosoul.Commands;

[CommandPrefix("player.weapon")]
public class PlayerWeapon : MonoBehaviour
{
    [SerializeField, Command(MonoTargetType.All)]   // player.weapon.damage
    float _damage = 3f;

    [Command(Description = "Fires once.")]          // player.weapon.fire
    public void Fire(int shots = 1) { ... }
}

public static class CheatCommands
{
    [Command("god", "Toggles god mode.")]           // explicit name, used as written
    private static bool GodMode { get; set; }
}
```

- Names default to the member name in kebab-case (`SetDamage` → `set-damage`, `_maxSpeed` → `max-speed`), after the class prefix.
  Prefixes of nested classes combine with their outer classes.
- Static members run directly. Instance members must be on a MonoBehaviour (any Component) and run on the active instances:
  `MonoTargetType.First` (default, one instance) or `MonoTargetType.All`. If there is none, the console shows a warning.
- Public and non-public methods, properties and fields are supported. Optional parameters can be left out, and overloads are chosen by argument count.
- Supported types: `string`, `char`, `bool`, all numeric types, enums, `Vector2/3/4`, `Vector2Int/3Int`, `Quaternion` (Euler angles), `Color`, `Color32`.
  Add more with a `ValueParser<T>` subclass; it is registered automatically.

## Using the console

| Input | Result |
|---|---|
| `player.weapon.fire 3` | Calls a method. Return values are printed. |
| `player.weapon.damage` | Prints the value (one line per object with `MonoTargetType.All`). |
| `player.weapon.damage = 5` | Sets the value. `player.weapon.damage 5` also works. |
| `spawn "big goblin" (1, 2, 0)` | Quotes group text with spaces, parentheses group vectors. |
| `tint = #FF8800` / `tint = orange` | Colors as hex or Unity color names. |
| `mode = god-mode` | Enums by name (case and dashes ignored) or number; flags as `A|B`. |

Default keys: **`** toggles, **Esc** closes, **Enter** runs, **Tab** completes the selected suggestion,
**Up/Down** select suggestions, or browse the history when no suggestions are shown. Click an error to see its stack trace.

Built-in commands: `help [command]`, `commands [filter]`, `clear`, `echo`, `time-scale`, `target-frame-rate`, `fps`, `log-level`, `quit`.

From code: `CommandAPI.Execute("time-scale = 0.5")`, `CommandAPI.Log("...")`, `CommandConsole.Instance.Open()`.

## Tools

- **Tools > Arosoul > Commands > Settings**: settings and validation.
- **Tools > Arosoul > Commands > Debugger**: every command grouped by class, with signature, target and setup problems.
  Double-click a command to open its script at the member.
- `Arosoul.Commands.Editor.CommandsAgentTools`: validate, describe, change settings and run commands from code. See SKILLS.md.

## Notes

- Commands are found by reflection over the assemblies that reference `Arosoul.Commands`, on first use. Test assemblies are skipped.
- `[Command]` derives from `PreserveAttribute`. With High managed stripping, also keep classes that are only used through commands
  with `[Preserve]` or a `link.xml`.

## Tests

Edit mode: naming, tokenizer, parsers, discovery, execution, autocomplete, history, editor tools.
Play mode: instance commands (`First`, `All`, no instance), the console component, pause and log mirroring.
