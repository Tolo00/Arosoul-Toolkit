# Arosoul Transitions

Extensible transition system built on UI Toolkit. Works on its own, and is used by Arosoul Scenes.

## Play a transition

```csharp
using Arosoul.Transitions;

TransitionHandle handle = TransitionAPI.Play(TransitionIds.Fade);

// Hold it open while you do work, then let it exit
var held = TransitionAPI.Play(TransitionParameters.Default(TransitionIds.LoadingScreen).WithHoldUntilReleased());
await held.WaitForPhaseAsync(TransitionPhase.Hold);
// ... work, held.SetProgress(0.5f, "Loading") ...
held.Release();
await held.WaitUntilFinishedAsync();
```

`TransitionIds` is generated (in the global namespace) into `Assets/Generated/Transitions` whenever a transition asset is created, renamed or deleted.
The generated file has no assembly definition, so it compiles into Assembly-CSharp. If your game code lives in its own assembly definitions,
add one in `Assets/Generated/Transitions` (referencing `Arosoul.Transitions`) and reference it from yours.
`TransitionId` fields are also drawn as a dropdown in the inspector. Ids are stable integers stored in the asset,
so renaming or reordering never breaks serialized references.

## Add a new transition

```csharp
[CreateAssetMenu(menuName = "Arosoul/Transitions/Wipe", fileName = "Wipe")]
public class WipeTransition : TransitionDefinition
{
    public override VisualElement Build() { /* create elements */ }
    public override void Apply(VisualElement visual, TransitionPhase phase, float t) { /* t is 0..1 */ }
}
```

Create an asset of it (Tools > Arosoul > Transitions > Transitions > New Transition). The same window shows each transition's type and can delete the selected one. `TransitionIds.Wipe` appears after the next compile.
From code, `Arosoul.Transitions.Editor.TransitionsAgentTools` can create, rename, delete, validate and describe transitions (see SKILLS.md).
Override `PlayAsync` to drive the In/Out phases with your own animation library, and `OnProgress` to react to progress.

## Input blocking

`InputBlocker` is reference counted. Transitions (and scene operations) acquire a block while active, and the overlay swallows
UI Toolkit pointer input. For gameplay and navigation input, subscribe to `InputBlocker.BlockedChanged` or check `InputBlocker.IsBlocked`.

## Notes

- Built-in transitions: Fade, Slide, Loading Screen. They are created the first time the package is used.
- Time is unscaled, transitions play with `Time.timeScale = 0`.
- If no PanelSettings is assigned on the generated `TransitionRegistry`, one is created at runtime.
