# Arosoul Scenes

Scene management for Unity 6.6: slots, scene dependencies, readiness gating, queued operations and transitions
(from Arosoul Transitions).

## Setup

1. Open **Tools > Scene System > Scene Config**.
2. Optionally enable a **Core scene**. It is a dependency of every scene and never unloaded.
3. Register scenes: drag them onto the Scenes tab, use *Register Scene*, *Register All Scenes In Folder* (a folder under Assets, subfolders included), or the Register button next to any scene field. Then add their dependencies. The search bar filters the list by scene name.
4. Define slots.

Settings are stored in `ProjectSettings/ScenesSettings.asset` and baked into `Assets/Generated/Scenes`
(a catalog asset and `SceneIds.cs` with `SlotIds` and `SceneIds`).
The generated files have no assembly definition, so they compile into Assembly-CSharp. If your game code lives in its own assembly definitions,
add an assembly definition in `Assets/Generated/Scenes` (referencing `Arosoul.Scenes`) and reference it from yours.

The catalog is baked automatically. SceneIds.cs (SlotIds and SceneIds) is generated with the Rebuild SceneIds button in the top right of Scene Config. It is highlighted when the file is out of date.

## Use

```csharp
using Arosoul.Generated;
using Arosoul.Scenes;

await SceneAPI.Load(SlotIds.Main, SceneIds.Gameplay, setActive: true)
    .WithTransition(TransitionIds.LoadingScreen)
    .WithPayload(levelData)
    .PerformAsync();

SceneAPI.Unload(SlotIds.Ui).Perform();
```

Scene fields in your components use the scene picker:

```csharp
[SerializeField] private SceneRef _nextLevel;
```

## Rules

- A loaded scene is kept while it is in a slot, is the core scene, or is a dependency of a kept scene.
- Dependencies load first, in order. Each scene is initialized before the next one loads.
- Operations are queued and run one at a time. A failed operation does not stop the queue; check `State` and `Error`.
- A scene can only be in one slot at a time.

## Scene readiness gating

```csharp
public class LevelBuilder : MonoBehaviour, ISceneInitializer
{
    public int InitializationOrder => 0; // lower runs first, equal runs in parallel
    public async Awaitable InitializeAsync(SceneContext context)
    {
        var data = context.GetPayload<LevelData>();
        await BuildLevelAsync(data);
    }
}
```

The operation (and its transition) waits for every initializer in the scene.

## Editor play mode

With *Play Mode Bootstrap* enabled, pressing play in a registered scene with dependencies starts an empty bootstrap scene.
At startup the runtime loads the scene you were editing through Scenes, so core and dependencies load first, in order.
A start scene you set yourself is never replaced.

In builds the scene Unity starts with is adopted into the initial slot and its missing dependencies load after it.
For deterministic Awake order in builds, make build index 0 a minimal boot scene and load everything else through Scenes.

## Tools

- **Tools > Scene System > Scene Config**: settings and validation.
- **Tools > Scene System > Scene Debugger**: live view in play mode (loaded scenes and why, slots, operations, transition, input blocking, log).
- Validation also runs on every bake and before builds; errors fail the build.

## Tests

Edit mode: graph, planner, validator and data. Play mode: the scene manager against a fake `ISceneLoader`.
Replace `ISceneLoader` to load scenes from another source.
