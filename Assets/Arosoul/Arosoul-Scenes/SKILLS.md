# SKILLS.md: Arosoul Scenes

Guide for AI agents working with, extending, or writing code against `com.arosoul.scenes`.

- **Package version:** 1.0.0
- **Unity version:** 6000.6 (Unity 6.6)
- **Language/UI:** C#, UI Toolkit for editor tooling.
- **Assemblies/namespaces:** runtime `Arosoul.Scenes`; editor `Arosoul.Scenes.Editor`.
- **Dependency:** requires `com.arosoul.transitions` 1.0.0. Runtime scene operations use its `InputBlocker` and transition API. Do not duplicate those services.
- **Generated API:** `SlotIds` and `SceneIds` in the **global namespace** (no `using` needed; `using Arosoul.Scenes;` is still needed for `SceneAPI`, `SlotId` and `SceneRef`).
- **Editor automation:** `Arosoul.Scenes.Editor.ScenesAgentTools` configures everything the Scene Config window does from code (see section 3.1). Prefer it over driving the window.

For transition definition authoring, priority, handle lifecycle, and `InputBlocker` details, also read the Transitions package's `SKILLS.md`. This file describes how Scenes uses those systems.

---

## 1. Mental model and architecture

- **`SceneRef`** is a serializable scene reference identified by the Unity asset GUID. Cached path/name fields are editor/runtime display data; moving or renaming the scene does not change its GUID.
- **`SlotId`** identifies a configured slot. A slot contains at most one scene. Loading a new scene into an occupied slot replaces that slot's previous scene.
- **Registered scene** means a scene listed in Scene Config. A requested root scene must be registered before it can be loaded through `SceneAPI`.
- **Dependencies** are loaded transitively before the scene that needs them. A shared dependency is loaded once. A referenced dependency may itself be unregistered, but it then has no configured dependency list of its own and the validator warns about it.
- **Core scene** (optional) is an implicit dependency of every non-core scene. It is never unloaded and must not itself have dependencies.
- **Keep rule**: a loaded scene stays loaded while it occupies a slot, is the core scene, or is a dependency of another kept scene. Other loaded scenes are unloaded during the next operation. There are no manual dependency reference counts.
- **`SceneOperation`** describes one set of slot changes. The planner evaluates all its loads/unloads together, computes the final slot state, then determines what must load and unload.
- **`SceneManager`** processes operations in FIFO order, one at a time. A failed operation does not stop later queued operations.
- **Readiness gating**: `ISceneInitializer` components run after a scene is loaded by an operation. The operation waits for initialization before loading the next scene in the dependency chain.

Runtime flow:

```text
SceneAPI.Load / Unload / NewOperation
    -> SceneOperation (fluent description)
    -> SceneManager queue
    -> ScenePlanner (final slots, load/unload plan)
    -> unload dependents first
    -> load dependencies first; initialize each loaded scene
    -> optional UnloadUnusedAssets
    -> release transition/input block and finish operation
```

## 2. Settings, catalog, and generated files

| What | Location |
|---|---|
| Editable project settings | `ProjectSettings/ScenesSettings.asset` |
| Runtime catalog (preloaded asset) | `Assets/Generated/Scenes/SceneCatalog.asset` |
| Generated `SlotIds` and `SceneIds` | `Assets/Generated/Scenes/SceneIds.cs` |
| Editor play-mode bootstrap scene | `Assets/Generated/Scenes/PlayModeBootstrap.unity` |

Editor menu items:

- **Tools > Arosoul > Scenes > Scene Config**: configure core scene, registered scenes/dependencies, slots, validation, editor bootstrap, and build-settings syncing.
- **Tools > Arosoul > Scenes > Scene Debugger**: inspect loaded scenes, slots, queued/current operations, transitions, input-block reasons, and the event log while playing.
- **Tools > Arosoul > Scenes > Rebuild Catalog**: save/commit settings and bake the runtime catalog.

**Never hand-edit `ProjectSettings/ScenesSettings.asset`, `Assets/Generated/Scenes/SceneCatalog.asset`, `SceneIds.cs`, or the generated bootstrap scene.** Use Scene Config and the provided menu/buttons; generated assets are maintained by the editor tooling.

The runtime catalog is baked automatically when Scene Config commits settings and is added to Player Settings' preloaded assets. `SceneIds.cs` is a separate generated source file:

- Every `ScenesAgentTools` method that changes settings also regenerates `SceneIds.cs`. `ScenesAgentTools.Commit()` saves, bakes and regenerates in one call.
- In the Scene Config window, rebuild it with the **Rebuild SceneIds** button when it is marked out of sync. The window does not regenerate it automatically (to avoid a recompile per edit), and the **Rebuild Catalog** menu command does not regenerate it either.
- Regenerating `SceneIds.cs` triggers a script compilation. Code that references new `SlotIds`/`SceneIds` members compiles only after that.

Generated source has no assembly definition and compiles into `Assembly-CSharp` by default. Code inside a project `.asmdef` cannot reference types compiled into `Assembly-CSharp`. If an `.asmdef` assembly needs `SlotIds` or `SceneIds`, add an appropriate `.asmdef` beside the generated source (referencing `Arosoul.Scenes`) and reference that generated assembly from the game assembly. Check existing parent/nested assembly definitions before adding one.

Generated members:

- `SlotIds.<Key>` names come from slot keys.
- `SceneIds.<Name>` names come from registered scene names (the scene file name without `.unity`).
- Names are sanitized to valid C# identifiers; collisions receive numeric suffixes. A scene rename can therefore change its generated member name even though its `SceneRef` GUID remains stable.
- Slot ids are stored integers. Renaming a slot key changes the generated constant name, not the stored numeric id. Slot ids are allocated from a persisted next-id counter and are not intentionally reused when slots are removed.

## 3. Configure the system in the Editor

Open **Tools > Arosoul > Scenes > Scene Config**. Its tabs are:

- **General**: optional core scene, initial slot, Play Mode Bootstrap, and Sync Build Settings.
- **Scenes**: register scenes, configure dependencies, search by scene name, drag scenes from the Project window, or use **Register All Scenes In Folder** (includes subfolders).
- **Slots**: create slots and edit their keys.
- **Validation**: review configuration errors and warnings; **Validate and Rebuild** commits the settings and bakes the catalog.

On first use, the settings utility creates a `Main` slot and selects it as the initial slot when no slots exist. The initial slot is still required; validation reports an error if it is missing or references a deleted slot.

Registration and dependency rules:

- Register a scene before using it as a root in `SceneAPI.Load` or assigning its dependency list.
- Dependencies should also be registered when they have dependencies of their own. An unregistered dependency can still be loaded, but it is treated as a leaf and validation reports a warning.
- The core scene is configured separately. It is injected as a dependency of every non-core scene, is never unloaded, and cannot have dependencies itself.
- Dependency cycles, deleted scene references, self-dependencies, duplicate scene registrations, duplicate slot keys, and missing/invalid initial slots are validation errors.
- If Sync Build Settings is enabled, referenced scenes are added to Build Settings, disabled existing entries are enabled, and entries of deleted scenes are removed. Existing build-settings order is preserved. With syncing disabled, the validator warns when a **registered scene** is missing from or disabled in Build Settings. It does not explicitly check the build-state inclusion of every core/dependency reference in that mode, so verify those paths manually for player builds.

Changes in the Scene Config window are saved to Project Settings and the catalog is baked after a short debounce. Scene ids are rebuilt separately. The catalog bake is deferred while in Play Mode and performed after returning to Edit Mode.

### 3.1 Editor automation API (`ScenesAgentTools`)

`Arosoul.Scenes.Editor.ScenesAgentTools` is a public, editor-only static API for agents, editor scripts and `-executeMethod` wrappers. Scenes are identified by their project relative path (`Assets/Scenes/Level.unity`), slots by their key. Every method that changes settings records an undo step, saves `ProjectSettings/ScenesSettings.asset`, bakes the catalog (and syncs Build Settings if enabled) and regenerates `SceneIds.cs` before it returns. Invalid input throws `ArgumentException`/`InvalidOperationException` with an actionable message.

```csharp
using Arosoul.Scenes.Editor;

ScenesAgentTools.SetCoreScene("Assets/Scenes/Core.unity");
ScenesAgentTools.RegisterScene("Assets/Scenes/Managers.unity");
ScenesAgentTools.RegisterScene("Assets/Scenes/Gameplay.unity", new[] { "Assets/Scenes/Managers.unity" });
ScenesAgentTools.AddSlot("Ui");
ScenesAgentTools.SetInitialSlot("Main");

string[] diagnostics = ScenesAgentTools.Validate(); // fix every "ERROR:" entry, review "WARNING:" entries
string summary = ScenesAgentTools.Describe();       // readable dump of the whole configuration
```

| Method | Use |
|---|---|
| `Data` | Read-only view of the current `ScenesData`. |
| `RegisterScene(path, dependencyPaths = null)` | Register a scene; with `dependencyPaths` its dependency list is replaced. Already registered scenes only get their dependencies updated. Returns the `SceneRef`. |
| `RegisterScenesInFolder(folder)` | Register every scene in a folder and its subfolders; returns the number added. |
| `UnregisterScene(path)` | Remove a registered scene. Other scenes that depend on it keep the reference. |
| `SetDependencies(path, dependencyPaths)` | Replace the ordered dependency list of a registered scene. Self-dependencies are rejected. |
| `SetCoreScene(path)` | Enable and assign the core scene; `null` disables it. |
| `AddSlot(key)` / `RenameSlot(key, newKey)` / `RemoveSlot(key)` | Manage slots. Keys must be unique. Renaming keeps the stored id. |
| `SetInitialSlot(key)` / `GetSlot(key)` | Set the startup slot / look up a slot id by key (`SlotId.None` if missing). |
| `SetOptions(playModeBootstrap, syncBuildSettings)` | Change the editor options; `null` keeps a value. |
| `Commit()` | Save, bake and regenerate `SceneIds.cs`; returns diagnostics. Use after renaming/moving scene files. |
| `Validate()` | `ERROR:`/`WARNING:` diagnostics, including a stale `SceneIds.cs` or missing catalog. Empty means valid. |
| `Describe()` | Text summary: options, core, initial slot, slots and scenes with their generated member names and dependencies. |

The window listens to these changes (`ScenesSettingsUtility.Changed`) and refreshes when open. For batch mode, put a parameterless `public static` wrapper in an editor assembly and call it with `-executeMethod`.

## 4. Loading and unloading scenes

```csharp
using Arosoul.Scenes;

// SlotIds, SceneIds and TransitionIds are generated in the global namespace.
await SceneAPI.Load(SlotIds.Main, SceneIds.Gameplay, setActive: true)
    .WithTransition(TransitionIds.LoadingScreen)
    .WithPayload(levelData)
    .PerformAsync();

SceneAPI.Unload(SlotIds.Main).Perform();
```

Entry points:

- `SceneAPI.Load(slot, scene, setActive = false, reload = false)`.
- `SceneAPI.Unload(slot)`.
- `SceneAPI.NewOperation()` for multiple changes in one operation.

`SceneOperation` builder methods:

| Method | Behaviour |
|---|---|
| `Load(slot, scene, setActive = false, reload = false)` | Put the scene in the target slot; replace its previous scene. `reload` forces unload/reload if the target is already loaded. |
| `Unload(slot)` | Empty the slot; scenes no longer required by any kept scene are unloaded. |
| `WithTransition(TransitionParameters)` | Cover the operation with a transition. A `TransitionId` implicitly converts to default parameters. |
| `WithUnloadUnusedAssets(bool = true)` | Run `Resources.UnloadUnusedAssets()` after scene changes when enabled. |
| `WithPayload(object)` | Pass one payload object to initializers of every scene loaded by this operation, including dependencies. |
| `WithInputBlocking(bool)` | Enable/disable the operation's own `InputBlocker` token. Default is `true`. |

Several loads/unloads can be chained. They are not executed as separate sequential mini-operations: the planner validates and plans the final slot state for the whole operation. This allows a scene to move between slots within one operation when the old slot is also unloaded, for example `SceneAPI.NewOperation().Unload(SlotIds.Main).Load(SlotIds.UI, SceneIds.Hud)`. A scene cannot occupy two slots in the final state.

Execution:

- `Perform()` queues the operation and returns immediately.
- `PerformAsync()` queues it and awaits its finish. It does not throw for an ordinary operation failure; inspect `State` and `Error`.
- An operation may be performed only once. Builder methods only change it while its state is `Created`; calling them after `Perform` logs a warning and has no effect.
- `Cancel()` can cancel an operation in `Created` or `Queued` state. It cannot cancel an operation that is already running. A cancelled queued item is skipped when it reaches the front of the queue.
- Queue order is FIFO. Operations do not run in parallel. A failed operation does not stop the queue.
- Operations are **not transactional**. If an exception occurs after some scenes have loaded or unloaded, those completed changes are not rolled back automatically. Inspect current loaded scenes/slots and issue another operation if reconciliation is needed.

Operation state:

- `SceneOperationState`: `Created`, `Queued`, `Running`, `Completed`, `Failed`, `Cancelled`.
- `SceneOperationPhase`: `None`, `Planning`, `TransitionIn`, `Unloading`, `Loading`, `Initializing`, `UnloadingAssets`, `TransitionOut`, `Done`.
- Other useful members: `Progress` (0–1), `Error`, `IsFinished`, `Finished`, and `Describe()`.
- `Finished` is raised for completed, failed, or cancelled operations. `SceneEvents.OperationFailed` is specifically for failures, not cancellation.

Query runtime state:

```csharp
SceneAPI.IsBusy;
SceneAPI.IsLoaded(SceneIds.Gameplay);
SceneAPI.IsSlotOccupied(SlotIds.Main);
SceneAPI.TryGetSceneInSlot(SlotIds.Main, out LoadedSceneInfo info);
IReadOnlyList<LoadedSceneInfo> loaded = SceneAPI.LoadedScenes;
```

`LoadedSceneInfo` includes `Guid`, `Name`, `Path`, `Scene`, `Slot`, `IsCore`, `IsDependencyOnly`, `LoadDuration`, and `InitializationDuration`. A dependency-only scene has `SlotId.None` and `IsDependencyOnly == true`.

Planning guarantees:

- Requested root scenes must be registered. An unregistered root fails validation/planning and the operation fails.
- The graph includes the core as an implicit dependency of every non-core scene.
- Dependencies load before dependents; dependents unload before their dependencies.
- Shared dependencies are loaded once and kept while any slot/core/dependent still needs them.
- An empty operation reconciles the current slot/core state with missing dependencies; startup uses this mechanism.
- `setActive: true` sets the loaded target scene active. Avoid direct `UnityEngine.SceneManagement.SceneManager.LoadScene`/`UnloadScene` calls for scenes owned by this package.

## 5. Scene references and readiness gating

### Scene and slot fields

```csharp
[SerializeField] private SceneRef _nextLevel;
[SerializeField] private SlotId _targetSlot;
```

- `SceneRef` equality is by GUID. Use generated `SceneIds` in code and the scene picker in serialized fields.
- `SceneRef` fields display a **Register** button when their referenced scene is not yet registered.
- `SlotId` fields display a dropdown of configured slots. Use generated `SlotIds`; do not construct ids manually or store scene names as strings.

### Initializers

Implement `ISceneInitializer` on a `MonoBehaviour` when a newly loaded scene must complete asynchronous setup before the next dependent scene loads or the operation exits its transition.

```csharp
public sealed class LevelBuilder : MonoBehaviour, ISceneInitializer
{
    public int InitializationOrder => 0; // Lower orders run before higher orders.

    public async Awaitable InitializeAsync(SceneContext context)
    {
        if (context.TryGetPayload<LevelData>(out var data))
            await BuildLevelAsync(data);
    }
}
```

Current implementation semantics:

- `UnitySceneLoader` finds `ISceneInitializer` on scene `MonoBehaviour`s, including inactive objects and children of root objects.
- Initializers are grouped by `InitializationOrder`. Lower order groups run to completion before higher groups start. Initializers in the same group are started before the manager awaits them, so they run concurrently.
- Synchronous throws and asynchronous failures are collected. The manager still awaits the other initializers/groups, then fails the scene operation if any initializer failed.
- A scene is registered in `LoadedScenes` and `SceneEvents.SceneLoaded` is raised **before** its initializers finish. Do not treat `SceneLoaded` as a readiness-complete event; observe the operation phase/state when readiness matters.
- A scene already loaded and retained by the planner is not initialized again unless it is explicitly reloaded.
- **Startup scene:** the scene Unity starts with in a build (or in the editor without the bootstrap) is adopted into the initial slot. The reconciliation operation loads its missing dependencies, initializing each, and then runs the adopted scene's own initializers. The adopted scene's `Awake`/`OnEnable`/`Start` still ran before its dependencies were loaded, so keep dependency lookups in `InitializeAsync`, not `Awake`.

`SceneContext` exposes `Guid`, `Name`, `Slot`, `Payload`, and `HasPayload`. `TryGetPayload<T>` reports whether the payload is of the requested type. `GetPayload<T>` returns `default` when the payload is missing or has a different type. The same operation payload is supplied to every scene loaded by that operation, including dependency-only scenes; their `Slot` is normally `SlotId.None`.

## 6. Events and observing operations

`SceneEvents` is static and exposes:

- Operation: `OperationQueued`, `OperationStarted`, `OperationCompleted`, `OperationFailed`, `PhaseChanged`, `ProgressChanged`.
- Scene lifecycle: `SceneLoaded`, `SceneUnloading`, `SceneUnloaded`.

`SceneLoaded` is fired after the Unity scene is loaded and tracked, but before `ISceneInitializer` work completes. `SceneUnloading` fires before calling the loader; `SceneUnloaded` fires after the scene has been removed from the manager's tracked collections.

Static subscriptions are cleared at `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`. Subscribe in `OnEnable` and unsubscribe in `OnDisable`:

```csharp
private void OnEnable() => SceneEvents.OperationCompleted += HandleCompleted;
private void OnDisable() => SceneEvents.OperationCompleted -= HandleCompleted;
```

`SceneOperation.Finished` is an instance event for that specific operation. `SceneEventLog` is a 100-entry ring buffer used by the debugger and is reset at subsystem registration.

## 7. Transitions and input blocking

A scene operation with `WithTransition(...)` holds the transition open while unloading, loading, and initializing scenes:

1. The transition enters In and reaches Hold.
2. The operation performs scene changes and forwards progress through `TransitionHandle.SetProgress` (for example, `"Loading Gameplay"`).
3. The operation releases the handle; the transition plays Out.
4. The operation finishes after the transition has completed, unless that transition was cancelled/interrupted.

Rules:

- The operation forces `HoldUntilReleased` on its transition parameters.
- If the supplied transition priority is `0`, Scenes changes it to `SceneOperation.DefaultTransitionPriority` (`100`). Non-zero priorities are kept as supplied.
- If the transition is rejected or interrupted, the operation continues without waiting for that transition.
- If the scene operation fails after the transition has reached Hold, it still releases the transition and releases its own input block.
- `WithInputBlocking(true)` (the default) acquires an operation-owned `InputBlocker` token until the operation is done. A transition definition with `BlockInput` enabled can acquire a separate token as well. `WithInputBlocking(false)` disables only the operation-owned token; it does not override the transition asset's `BlockInput` setting.
- `InputBlocker` does not disable gameplay or keyboard/gamepad actions. The project's input layer must react to `InputBlocker.BlockedChanged` or check `InputBlocker.IsBlocked`. See the Transitions guide for the overlay pointer-blocking caveat, including the current initialization gap: if the first `TransitionManager` overlay is created after this operation has already acquired its default block, the overlay may not reflect the existing blocked state until that state changes.

## 8. Editor play mode, builds, and validation

### Editor Play Mode Bootstrap

With **Play Mode Bootstrap** enabled, when the active edit-mode scene is registered and has dependencies (including an enabled core scene), the editor redirects Play Mode to an empty generated bootstrap scene. At runtime, Scenes loads the scene being edited through an operation, so dependencies load and initialize first. A user-assigned `EditorSceneManager.playModeStartScene` is respected and is not replaced.

If the active scene is not registered, has no dependencies, or bootstrap is disabled, no redirect is set up.

### Build startup

In a player build, Unity loads Build Settings scene 0 normally. Scenes adopts that active scene into the configured initial slot when the scene path is present in the catalog, then runs an empty reconciliation operation to load missing dependencies and the core scene, and finally runs the adopted scene's `ISceneInitializer`s. The already-loaded startup scene has already run its Unity `Awake` methods before those dependencies are loaded, so their relative `Awake` ordering is not deterministic.

For deterministic startup ordering, make build index 0 a minimal boot scene and load the initial gameplay scene through `SceneAPI`.

### Build settings and validation

- With **Sync Build Settings** enabled, catalog baking adds all known referenced scene paths (registered scenes, core, and dependencies) to Build Settings, enables existing disabled entries and removes entries whose scene file no longer exists. The order of the remaining entries is preserved. Unreferenced but existing entries are never removed.
- Validation runs when settings are committed and before builds. Configuration errors fail the build; warnings do not.
- A stale `SceneIds.cs` is reported as a warning before build; the build hook does not regenerate the source file automatically. Rebuild it in Scene Config or call `ScenesAgentTools.Commit()`.
- Catalog bake is deferred while playing and is performed after returning to Edit Mode.
- If there is no runtime `SceneCatalog`, `SceneBootstrap` leaves the system dormant; no `SceneManager` is initialized by startup.

## 9. Rules, gotchas, and extension limits

- Do not use scene-name strings or Unity's `SceneManager.LoadScene`/`UnloadScene` for scenes managed by this package. Those calls bypass slot ownership, dependency planning, keep rules, events, and readiness gating.
- Only registered scenes can be requested as load roots. A dependency can be unregistered, but then it cannot declare its own dependencies; validation warns about this.
- A scene can occupy only one slot at a time. A plan that leaves the same scene in two slots fails.
- The core scene cannot have dependencies. Dependency cycles are validation/planning errors.
- Do not hand-edit `SceneIds.cs`, `SceneCatalog.asset`, `PlayModeBootstrap.unity`, or `ProjectSettings/ScenesSettings.asset`.
- `Awaitable` instances are single-use; do not await the same instance twice.
- Static state is reset using `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` so the packages work with domain reload disabled. New static state must follow the same pattern.
- Editor commits must not create Undo entries. Keep the existing distinction: automated commits use `ApplyModifiedPropertiesWithoutUndo`; user changes use `ScenesUndo.Apply`.
- `SceneRef` references survive scene moves/renames because the GUID is authoritative. Generated member names may change when the scene filename changes.

### Custom loading backend

`ISceneLoader` defines the backend contract (`LoadAdditiveAsync`, `UnloadAsync`, `SetActive`, `UnloadUnusedAssetsAsync`, `GetInitializers`). `UnitySceneLoader` is the default runtime implementation and the Play Mode tests use `FakeSceneLoader`.

Replace it with `SceneAPI.SetLoader(loader)` (or `SceneManager.Inst.SetLoader`), for example to load scenes through Addressables:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void InstallLoader() => SceneAPI.SetLoader(new AddressablesSceneLoader());
```

- Replacing is only allowed while no operation is queued or running; otherwise it logs an error and returns `false`. Install the loader before the first operation (startup adoption runs at `AfterSceneLoad`, so `BeforeSceneLoad` is early enough).
- Scenes that are already loaded stay tracked and are later unloaded through the new loader, so it must be able to unload scenes it did not load itself (the startup scene, the editor bootstrap scene).
- The current loader is exposed as `SceneManager.Inst.Loader`.

## 10. Extending and testing

Extension points currently available:

- **Scene graph logic:** `SceneGraph` resolves dependency order, injects the core dependency, and detects cycles. It contains no Unity scene-loading calls.
- **Operation planning:** `ScenePlanner` computes final slots and load/unload sets without Unity scene APIs.
- **Validation:** `ScenesValidator` validates settings against an `ISceneProjectContext`; its editor implementation queries the AssetDatabase and Build Settings.
- **Loading backend:** implement `ISceneLoader` and install it with `SceneAPI.SetLoader` (see above).
- **Editor automation:** `ScenesAgentTools` (section 3.1) wraps every configuration change; add new automation there rather than writing to `ScenesSettings` directly.

Test coverage in the package:

- `Tests/Editor`: dependency graph ordering, shared/core dependencies, cycle detection, planner slot/load/unload/reload behaviour, validation, `ScenesData` cloning/cache behaviour, and `SceneRef` identity.
- `Tests/PlayMode`: core/dependency loading, keep rules, queue ordering/cancellation, failure handling, initializer ordering/concurrency/payload, input-block lifetime, events, unused-asset unloading, reload, operation progress, startup-scene adoption and initialization, and loader replacement using `FakeSceneLoader`.

Run both package test assemblies (`Arosoul.Scenes.Tests.Editor`, `Arosoul.Scenes.Tests.PlayMode`) after changing runtime or editor behaviour. Play Mode tests must be run asynchronously by automation bridges because entering Play Mode reloads the domain.

## 11. File map

```text
Arosoul-Scenes/
  Runtime/
    Ids/                  SceneRef, SlotId
    Data/                 ScenesData (settings model), SceneCatalog (baked runtime copy)
    Graph/                SceneGraph, ScenePlanner
    Loading/              ISceneLoader, UnitySceneLoader, ISceneInitializer, SceneContext
    Operations/           SceneAPI, SceneOperation, SceneManager, SceneEvents, SceneEventLog,
                          SceneBootstrap, LoadedSceneInfo, SceneOperationEnums
  Editor/
    ScenesAgentTools.cs   public editor automation API
    ScenesSettings.cs, ScenesSettingsUtility.cs, SceneCatalogBaker.cs,
    SceneBuildSettingsSync.cs, ScenesBuildHooks.cs, PlayModeBootstrap.cs, ScenesUndo.cs,
    Validation/           ScenesValidator, ValidationReport
    Drawers/              IdDrawers, SceneRefDrawer
    Windows/              SceneConfigWindow, SceneDebuggerWindow, EditorUI
  Tests/
    Editor/               graph, planner, validator, data
    PlayMode/              SceneManager against FakeSceneLoader
```

## 12. Code conventions

Match the existing implementation when adding or modifying code:

- Allman braces, 4-space indentation, and namespace matching the assembly (`Arosoul.Scenes` or `.Editor`).
- Private fields use `_camelCase`; public types, methods, and properties use `PascalCase`.
- Use `[SerializeField] private` for serialized private fields, as the existing package code does.
- Add `/// <summary>` XML documentation to public types and members; keep other comments short and explain non-obvious behaviour.
- Keep runtime code free of `UnityEditor` references. Editor-only code belongs in `Editor/` and the editor assembly.
- Preserve the assembly boundary and the dependency on Transitions; do not add a reverse dependency from Transitions to Scenes.
- Use `ScenesUndo.Apply` for user-authored editor changes and `ApplyModifiedPropertiesWithoutUndo` for automated commits.
