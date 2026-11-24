# SKILLS.md: Arosoul Scenes

Guide for AI agents working with, or writing code against, the `com.arosoul.scenes` package. Target: Unity 6.6, C#, UI Toolkit for all editor and in-game UI.

- Namespace / assembly: `Arosoul.Scenes` (runtime), `Arosoul.Scenes.Editor` (editor).
- Depends on the Arosoul Transitions package (`com.arosoul.transitions`). For writing transitions, transition handles and input blocking details, read `Arosoul-Transitions/SKILLS.md`. This file only covers how scene operations use them.
- Generated constants live in the namespace `Arosoul.Generated` (`using Arosoul.Generated;`).

---

## 1. Mental model

- **Scene** is referenced by `SceneRef` (GUID based, serializable). Never reference scenes by name strings.
- **Slot** (`SlotId`) holds at most one scene. Loading a scene into a slot replaces what was there. Slots are defined in Scene Config.
- **Registered scene**: only registered scenes can be loaded or have dependencies.
- **Dependencies**: a registered scene can list scenes that must be loaded before it. Resolved transitively, loaded in order, loaded once.
- **Core scene** (optional): implicit dependency of every scene, never unloaded, cannot have dependencies itself.
- **Keep rule**: a loaded scene stays loaded while it is in a slot, is the core scene, or is a dependency of a kept scene. Everything else is unloaded by the next operation. There are no manual refcounts.
- **Operation** (`SceneOperation`): a fluent description of changes. Operations are queued and run one at a time in the order performed.
- **Operation phases**: Planning, TransitionIn, Unloading, Loading (each scene is initialized right after it loads), UnloadingAssets, TransitionOut, Done.
- **Readiness gating**: components implementing `ISceneInitializer` run async setup after their scene loads. The operation waits for all of them before loading the next scene or leaving the transition.
- **Transition and input blocking**: an operation can be covered by a transition and blocks input while it runs (see section 4).

---

## 2. Where things live

| What | Location |
|---|---|
| Settings (editable) | `ProjectSettings/ScenesSettings.asset`, edited in **Tools > Scene System > Scene Config** |
| Runtime catalog (baked, preloaded asset) | `Assets/Generated/Scenes/SceneCatalog.asset` |
| Generated `SlotIds`, `SceneIds` | `Assets/Generated/Scenes/SceneIds.cs` |
| Editor play mode bootstrap scene | `Assets/Generated/Scenes/PlayModeBootstrap.unity` |

Menu items: `Tools/Scene System/Scene Config`, `Tools/Scene System/Scene Debugger`, `Tools/Scene System/Rebuild Catalog`.

**Never edit anything in `Assets/Generated` or `ProjectSettings/ScenesSettings.asset` by hand**, they are overwritten. The generated file has no assembly definition, so it compiles into Assembly-CSharp. Code in a project assembly definition cannot see `SlotIds` or `SceneIds` unless the user adds an assembly definition in `Assets/Generated/Scenes` (referencing `Arosoul.Scenes`) and references it.

---

## 3. Using the scene system

```csharp
using Arosoul.Generated;
using Arosoul.Scenes;
```

### Load a scene into a slot

```csharp
await SceneAPI.Load(SlotIds.Main, SceneIds.Gameplay, setActive: true)
    .WithTransition(TransitionIds.LoadingScreen)
    .PerformAsync();
```

- `Perform()` queues without waiting, `PerformAsync()` queues and waits.
- `PerformAsync()` does not throw when the operation fails. Check `operation.State` (`Completed`, `Failed`, `Cancelled`) and `operation.Error`.
- Dependencies and the core scene are loaded automatically, in order, before the scene.

### Builder methods (`SceneOperation`)

| Method | Effect |
|---|---|
| `Load(SlotId, SceneRef, setActive = false, reload = false)` | Load into a slot. `reload` unloads and loads again if already loaded. |
| `Unload(SlotId)` | Unload the slot's scene, plus dependencies nobody else needs. |
| `WithTransition(TransitionParameters)` | Cover the operation. Accepts a `TransitionId` directly (implicit conversion, default times). |
| `WithUnloadUnusedAssets(bool = true)` | Run `Resources.UnloadUnusedAssets` afterwards. |
| `WithPayload(object)` | Data handed to initializers via `SceneContext`. |
| `WithInputBlocking(bool)` | Default `true`. |
| `Cancel()` | Only works while queued. A running operation cannot be cancelled. |

Several `Load`/`Unload` calls can be chained in one operation. Planning is done for the whole operation at once. Also available: `State`, `Phase`, `Progress`, `Error`, `IsFinished`, `Finished` event, `Describe()`.

### Query state

```csharp
SceneAPI.IsBusy                                  // running or queued
SceneAPI.IsLoaded(SceneIds.Gameplay)
SceneAPI.IsSlotOccupied(SlotIds.Main)
SceneAPI.TryGetSceneInSlot(SlotIds.Main, out LoadedSceneInfo info)
SceneAPI.LoadedScenes                            // in load order
```

`LoadedSceneInfo`: `Guid`, `Name`, `Path`, `Scene`, `Slot`, `IsCore`, `IsDependencyOnly`, `LoadDuration`, `InitializationDuration`.

### Events (`SceneEvents`, static)

`OperationQueued`, `OperationStarted`, `OperationCompleted`, `OperationFailed`, `PhaseChanged(op, phase)`, `ProgressChanged(op, 0..1)`, `SceneLoaded`, `SceneUnloading`, `SceneUnloaded`. All subscriptions are cleared when entering play mode. Subscribe in `OnEnable`, unsubscribe in `OnDisable`.

### Scene and slot fields in components

```csharp
[SerializeField] private SceneRef _nextLevel;    // drawn as a scene picker
[SerializeField] private SlotId _targetSlot;     // drawn as a dropdown of defined slots
```

A `SceneRef` field shows a **Register** button if the scene is not registered yet.

### Async setup before a scene is ready (readiness gating)

```csharp
public class LevelBuilder : MonoBehaviour, ISceneInitializer
{
    public int InitializationOrder => 0;   // lower runs first, equal runs in parallel
    public async Awaitable InitializeAsync(SceneContext context)
    {
        var data = context.GetPayload<LevelData>();   // or TryGetPayload<T>
        await BuildLevelAsync(data);
    }
}
```

- Found on any (also inactive) component in the loaded scene.
- A dependency is fully initialized before the scene that needs it is loaded.
- An exception in an initializer fails the operation.
- `SceneContext`: `Guid`, `Name`, `Slot`, `Payload`, `HasPayload`.

---

## 4. Transitions and input blocking in operations

- `WithTransition(x)` plays the transition held open: In, then Hold while scenes unload, load and initialize, then Out. The operation completes after the transition finished.
- Operation progress is forwarded to the transition (`TransitionHandle.SetProgress`) with a message such as "Loading Gameplay", so `LoadingScreenTransition` shows it automatically.
- Transition priority: an operation raises a priority of 0 to `SceneOperation.DefaultTransitionPriority` (100). If the transition is rejected or interrupted, the operation continues without it.
- If the operation fails, the transition still exits and input is released.
- Unless `WithInputBlocking(false)`, the operation holds an `InputBlocker` block until it is done. The overlay only blocks UI Toolkit pointer input. Gameplay and navigation input must be disabled by the project's input layer through `InputBlocker.BlockedChanged`.

Details on `TransitionId`, `TransitionParameters`, handles and authoring new transitions are in the Transitions package guide.

---

## 5. Editor workflow (what the human does)

Agents cannot click through editor windows. When a task needs configuration, tell the user exactly which step to do.

1. **Tools > Scene System > Scene Config**:
   - **General**: core scene, initial slot, Play Mode Bootstrap, Sync Build Settings.
   - **Scenes**: register scenes (drag and drop, *Register Scene*, *Register All Scenes In Folder*), set dependencies, search by name.
   - **Slots**: define slots. The key becomes `SlotIds.<Key>`.
   - **Validation**: lists problems. Errors fail builds.
   - The window remembers the selected tab across recompiles.
2. After changing slots or registered scenes, press **Rebuild SceneIds** (bottom right of Scene Config, next to the error and warning count). It is highlighted when `SceneIds.cs` is out of sync and disabled when it matches. The catalog itself is baked automatically. `SceneIds` names come from scene file names, slot keys from the slot list.
3. **Tools > Scene System > Scene Debugger** (play mode): loaded scenes and why, slots, running and queued operations, active transition, input blocking, event log.

Settings edits support Undo/Redo with named entries.

**Editor play mode**: with Play Mode Bootstrap on, pressing play in a registered scene that has dependencies starts an empty bootstrap scene. The runtime then loads the edited scene through Scenes, so dependencies load first, in order. A `playModeStartScene` set by the user is never replaced.

**Builds**: the scene Unity starts with is adopted into the initial slot and its missing dependencies load after it (Awake order is not guaranteed). For deterministic ordering, make build index 0 a minimal boot scene and load everything else through `SceneAPI`. Registered scenes are added to the build settings automatically when Sync Build Settings is on. Validation errors fail the build, a stale `SceneIds.cs` only logs a warning.

---

## 6. Rules and gotchas

- Do not use string scene names or `UnityEngine.SceneManagement.SceneManager.LoadScene` for scenes managed here. It bypasses slots, dependencies and the keep rule.
- A scene can only be in one slot. Loading it into a second slot fails the operation.
- The core scene cannot have dependencies. Dependency cycles are errors (validation and plan time).
- An unregistered scene cannot be loaded (`Failed` operation, error logged). Register it in Scene Config first.
- Operations never run in parallel. A failed operation does not stop the queue.
- `Awaitable` instances can only be awaited once. Do not store and await them twice.
- Static state is reset in `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` methods so the package works with domain reload disabled. New static state must do the same.
- Generated ids are stable integers. Renaming a slot key changes the generated constant name, not the serialized value. Slot ids are never reused.
- Editor code must not record undo from automated saves (use `ApplyModifiedPropertiesWithoutUndo` for commits, `ScenesUndo.Apply` for user edits).
- The system stays dormant if no catalog exists (no Scene Config set up yet).

---

## 7. Extending

- **Different loading backend** (for example Addressables later): implement `ISceneLoader` (`LoadAdditiveAsync`, `UnloadAsync`, `SetActive`, `UnloadUnusedAssetsAsync`, `GetInitializers`) and assign it in `SceneManager`. The default is `UnitySceneLoader`. Tests use `FakeSceneLoader`.
- **Pure logic** (no Unity scene APIs, unit tested): `SceneGraph` (dependency order, cycle detection, core injection), `ScenePlanner` (what to unload and load), `ScenesValidator`.

---

## 8. File map

```
Arosoul-Scenes/
  Runtime/Ids/             SceneRef, SlotId
  Runtime/Data/            ScenesData (settings model), SceneCatalog (baked runtime copy)
  Runtime/Graph/           SceneGraph, ScenePlanner
  Runtime/Loading/         ISceneLoader, UnitySceneLoader, ISceneInitializer, SceneContext
  Runtime/Operations/      SceneAPI, SceneOperation, SceneManager, SceneEvents, SceneEventLog,
                           SceneBootstrap, LoadedSceneInfo, SceneOperationEnums
  Editor/                  ScenesSettings (ScriptableSingleton), ScenesSettingsUtility, SceneCatalogBaker,
                           PlayModeBootstrap, SceneBuildSettingsSync, ScenesBuildHooks, ScenesUndo,
                           Validation/ (ScenesValidator, ValidationReport),
                           Drawers/ (SceneRefDrawer, SlotIdDrawer),
                           Windows/ (SceneConfigWindow, SceneDebuggerWindow, EditorUI)
  Tests/Editor             graph, planner, validator, data
  Tests/PlayMode           SceneManager against FakeSceneLoader
```

---

## 9. Code style used in this codebase

Match it when adding code.

- Allman braces, 4-space indentation, `namespace Arosoul.<System>` matching the assembly.
- Private fields `_camelCase`, `[SerializeField] private` for serialized fields, `PascalCase` for methods and properties.
- `/// <summary>` XML docs on public types and members, short plain comments above non-obvious code.
- `#region` mainly to group big parts of code so they are easy to search and manage, API sections are fine too.
- Singletons do not have to use `LazyLoaded<T>`, decide per case.
- Reset statics through Unity's newer API hooks where the Unity version supports it.
- A static facade class (like `SceneAPI`) only where it fits, not required in every system.

---

## 10. Status

The code was written without being compiled in Unity. Pure logic was compiled and checked outside Unity. If something fails on first import, the most likely areas are the UI Toolkit editor windows (`TabView`, drag and drop events, array editors), the timing of `playModeStartScene` for the editor bootstrap, and the play mode tests.
