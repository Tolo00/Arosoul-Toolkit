# SKILLS.md: Arosoul Transitions

Guide for AI agents working with, or writing code against, the `com.arosoul.transitions` package. Target: Unity 6.6, C#, UI Toolkit for all editor and in-game UI.

- Namespace / assembly: `Arosoul.Transitions` (runtime), `Arosoul.Transitions.Editor` (editor).
- Works on its own. The Arosoul Scenes package depends on it, but nothing here depends on Scenes.
- Generated constants live in the namespace `Arosoul.Generated` (`using Arosoul.Generated;`).

---

## 1. Mental model

- **Transition**: a `ScriptableObject` (`TransitionDefinition`) that holds its settings and knows how to build and animate a UI Toolkit visual tree. Never uGUI.
- **Overlay**: `TransitionManager` (created on demand) shows the active transition in a runtime UI Toolkit panel with a very high sorting order.
- **Phases** (ordered): `Pending, In, Hold, Out, Completed, Cancelled`. `Hold` lasts at least `MinHoldTime` and, if requested, until the handle is released.
- **TransitionId**: type-safe reference to a definition. A serializable struct wrapping a stable integer stored in the definition asset. Use the generated constants in code (`TransitionIds.Fade`), a dropdown in the inspector.
- **Registry**: `TransitionRegistry` is a baked, preloaded asset listing all definitions, used for lookup at runtime.
- **Input blocking**: `InputBlocker` is a reference counted, static service. Transitions acquire a block while active.

---

## 2. Where things live

| What | Location |
|---|---|
| Transition registry (baked, preloaded asset) | `Assets/Generated/Transitions/TransitionRegistry.asset` |
| Generated `TransitionIds` | `Assets/Generated/Transitions/TransitionIds.cs` |
| Transition definition assets | `Assets/Generated/Transitions/Definitions/` (default location, assets can be created anywhere in `Assets`) |

Menu items: `Tools/Transition System/Transitions` (window) and `Tools/Transition System/Regenerate Ids`.

**Never edit anything in `Assets/Generated` by hand**, it is overwritten. The generated file has no assembly definition, so it compiles into Assembly-CSharp. Code in a project assembly definition cannot see `TransitionIds` unless the user adds an assembly definition in `Assets/Generated/Transitions` (referencing `Arosoul.Transitions`) and references it.

The built-in transitions (Fade, Slide, LoadingScreen) are created the first time the package is used in a project.

---

## 3. Playing transitions

```csharp
using Arosoul.Generated;
using Arosoul.Transitions;

TransitionHandle handle = TransitionAPI.Play(TransitionIds.Fade);
```

`TransitionAPI`: `Play(TransitionId)`, `Play(TransitionParameters)`, `IsPlaying`, `GetActive()`.

### Parameters

`TransitionParameters` is an immutable struct: `Id`, `InTime`, `MinHoldTime`, `OutTime`, `Priority`, `HoldUntilReleased`.

```csharp
TransitionParameters.Default(id)                       // times from the definition asset
TransitionParameters.Symmetric(id, 0.4f, 0.2f)         // same in and out time, optional min hold
p.WithTimes(inTime, holdTime, outTime)
 .WithPriority(5)
 .WithHoldUntilReleased()
```

A `TransitionId` converts implicitly to `TransitionParameters` with default times.

### Control flow with a handle

```csharp
var held = TransitionAPI.Play(TransitionParameters.Default(TransitionIds.LoadingScreen).WithHoldUntilReleased());
await held.WaitForPhaseAsync(TransitionPhase.Hold);    // screen is fully covered
held.SetProgress(0.5f, "Loading");                     // shown by transitions that support it
// ... do work ...
held.Release();                                        // allow the Out phase
await held.WaitUntilFinishedAsync();
```

`TransitionHandle`: `Phase`, `Progress`, `Message`, `IsFinished`, `IsReleased`, `Parameters`, `PhaseChanged` event, `Release()`, `SetProgress(float, string)`, `Cancel()`, `WaitForPhaseAsync(phase)`, `WaitUntilFinishedAsync()`. Cancelling removes the visuals immediately.

Static events on `TransitionManager`: `TransitionStarted`, `TransitionFinished`.

### Priority

A new transition only interrupts the active one if its priority is strictly higher. Otherwise the returned handle is already `Cancelled` and a warning is logged. Time is unscaled, transitions play at `Time.timeScale = 0`.

---

## 4. Add a new transition

1. Create a class deriving from `TransitionDefinition`.
2. Create an asset of it: **Tools > Transition System > Transitions > New Transition**, or via `[CreateAssetMenu]`.
3. A stable id is assigned and `TransitionIds.<Key>` is generated automatically (the key defaults to the asset name, editable on the asset). The constant appears after the next compile.

```csharp
[CreateAssetMenu(menuName = "Arosoul/Transitions/Wipe", fileName = "Wipe")]
public class WipeTransition : TransitionDefinition
{
    [SerializeField] private Color _color = Color.black;

    public override VisualElement Build()
    {
        var element = new VisualElement();
        StretchToFill(element);                       // protected helper on the base class
        element.style.backgroundColor = _color;
        element.pickingMode = PickingMode.Ignore;
        return element;
    }

    // phase is In, Hold or Out. t goes 0..1 within the phase (eased for In and Out by the curve on the asset).
    public override void Apply(VisualElement visual, TransitionPhase phase, float t)
    {
        // set the visual state for the phase
    }

    // optional:
    // public override void OnProgress(VisualElement visual, float progress, string message)
    // public override async Awaitable PlayAsync(VisualElement v, TransitionPhase p, float duration, CancellationToken ct)
}
```

Base class settings on every definition: `DefaultInTime`, `DefaultMinHoldTime`, `DefaultOutTime`, easing curve, `BlockInput`, key and id.

Rules for definitions:
- Create visuals only in `Build()`. It is called every time the transition plays.
- Keep `Apply` free of side effects and idempotent, it is also driven by the editor preview.
- The Hold phase is driven by the player (`Apply(Hold, t)` over `MinHoldTime`, then `OnProgress` while held). Only In and Out go through `PlayAsync`.
- In `PlayAsync` overrides (for example with LitMotion) use unscaled time and honor the cancellation token.
- Unity-serialized fields only, definitions are assets.

Built-in definitions: `FadeTransition`, `SlideTransition` (direction), `LoadingScreenTransition` (progress bar, message, optional tips).

---

## 5. Input blocking

```csharp
using (InputBlocker.Acquire("My reason")) { /* ... */ }   // IDisposable token
InputBlocker.IsBlocked
InputBlocker.BlockCount
InputBlocker.GetReasons()
InputBlocker.BlockedChanged += blocked => { /* ... */ };
```

- While any block is held the overlay swallows UI Toolkit pointer events.
- It does **not** block keyboard or gamepad navigation, or gameplay input. The project's input layer must subscribe to `BlockedChanged` (or check `IsBlocked`) and disable its actions.
- Disposing a token twice is safe. All subscriptions and tokens are cleared when entering play mode.

---

## 6. Editor workflow (what the human does)

Agents cannot click through editor windows. When a task needs configuration, tell the user which step to do.

**Tools > Transition System > Transitions**:
- Left list: transition key followed by its type name in grey (the type is not part of the key).
- Right: the inspector with a live preview (Play button). Custom `PlayAsync` overrides are previewed through `Apply` only.
- **New Transition**: menu of all non-abstract `TransitionDefinition` types.
- **Delete Selected**: asks for confirmation, then deletes the asset. This cannot be undone.
- **Regenerate Ids**: forces a refresh.
- A warning is shown when two keys are duplicates (generated constants get a numeric suffix).

Ids, the registry and `TransitionIds.cs` refresh automatically when a definition asset is created, changed, moved or deleted, and before every build. `TransitionId` fields in any inspector are drawn as a dropdown of all transitions.

---

## 7. Rules and gotchas

- Always use `TransitionId` (generated constants or inspector dropdown). Never identify transitions by string or index.
- Do not reuse or hand-assign the integer ids. They are allocated once and stored in the assets. Renaming a key changes the generated constant name, not the serialized value. A duplicated asset gets a new id automatically.
- The runtime `PanelSettings` is created in code if none is assigned on the registry. If Unity logs a missing theme warning, assign a `PanelSettings` asset on `TransitionRegistry`.
- `Awaitable` instances can only be awaited once. Do not store and await them twice.
- Static state is reset in `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` methods so the package works with domain reload disabled. New static state must do the same.
- Do not touch `Assets/Generated/Transitions` in patches or reviews.

---

## 8. Extending

- **Custom animation library**: override `PlayAsync` in a definition.
- **Other panel**: assign a `PanelSettings` asset on the registry to control scaling, theme or sorting order.
- **Shared base class**: `LazyLoaded<T>` (public, in `Arosoul.Transitions`) creates a `DontDestroyOnLoad` MonoBehaviour on first access. Derived classes must call `ClearInstance()` from a `SubsystemRegistration` method.
- **Pure logic** (unit tested, no Unity APIs): `IdentifierUtility`, `StableIdAllocator`, `IdCodeGenerator`.

---

## 9. File map

```
Arosoul-Transitions/
  Runtime/Core/                 TransitionAPI, TransitionManager, TransitionHandle, TransitionDefinition,
                                TransitionParameters, TransitionId, TransitionPhase, TransitionRegistry, InputBlocker
  Runtime/DefaultTransitions/   FadeTransition, SlideTransition, LoadingScreenTransition
  Runtime/Shared/               LazyLoaded<T>
  Editor/                       TransitionRegistryBuilder (ids, registry, codegen), TransitionsWindow,
                                TransitionDefinitionEditor (preview), Drawers/ (TransitionIdDrawer, IdDropdownDrawer),
                                Generation/ (IdentifierUtility, StableIdAllocator, IdCodeGenerator, GeneratedFileUtility)
  Tests/Editor                  ids, parameters, input blocker, default transitions
  Tests/PlayMode                playback, hold until released, priority
```

---

## 10. Code style used in this codebase

Match it when adding code.

- Allman braces, 4-space indentation, `namespace Arosoul.<System>` matching the assembly.
- Private fields `_camelCase`, `[SerializeField] private` for serialized fields, `PascalCase` for methods and properties.
- `/// <summary>` XML docs on public types and members, short plain comments above non-obvious code.
- `#region` mainly to group big parts of code so they are easy to search and manage, API sections are fine too.
- Singletons do not have to use `LazyLoaded<T>`, decide per case.
- Reset statics through Unity's newer API hooks where the Unity version supports it.
- A static facade class (like `TransitionAPI`) only where it fits, not required in every system.

---

## 11. Status

The code was written without being compiled in Unity. Pure logic was compiled and checked outside Unity. If something fails on first import, the most likely areas are the runtime `PanelSettings` and theme used by the overlay, the UI Toolkit editor window and inspector, and the play mode tests that create the overlay.
