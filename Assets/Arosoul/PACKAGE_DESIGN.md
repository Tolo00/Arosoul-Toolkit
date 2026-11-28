---
name: arosoul-package-design
description: Conventions for designing a new Arosoul package or tool in this repository. Covers folder layout, where editor and runtime data is stored, baking and generated files, runtime entry points, editor windows, extensibility, agent tools, tests and docs. Read it before designing or scaffolding a new package under Assets/Arosoul.
---

# Arosoul package design guide

Patterns shared by the Arosoul packages (best examples: **Arosoul-Commands**, **Arosoul-Scenes**, **Arosoul-Transitions**).
Read this before designing a new package or tool under `Assets/Arosoul`.

**Everything here is optional.** Use a pattern only when the package needs what it solves. A small utility may need nothing
but a runtime folder and an asmdef. Don't add settings windows, baked assets or generated code "for consistency".
Section 0 helps you decide.

Code style comes from the user's global CLAUDE.md (Allman braces, `_camelCase` private fields, `[SerializeField]` without `private`,
SaintsField for inspector attributes, and so on). When editing an existing package, match the style that package already uses.

---

## 0. Decide what the package needs

| If the package… | Use | Section |
|---|---|---|
| has project-wide configuration edited in the editor | ScriptableSingleton settings in `ProjectSettings/` | 2.1 |
| needs that configuration at runtime | a baked runtime asset in `Assets/Generated/<Package>/`, preloaded | 2.2, 3 |
| lets designers author many items (definitions, presets) | ScriptableObject assets in the user's folders, collected into a registry | 2.3 |
| is referenced from code by name (scenes, transitions, actions) | generated id constants | 4 |
| needs an object in the scene (manager, overlay) | a prefab plus a `GameObject > Arosoul > …` menu item, or auto-creation | 5.2 |
| lets users extend it with their own types or members | attributes or base classes found by reflection | 7 |
| can be misconfigured | `Validate()` with ERROR/WARNING diagnostics, checked before builds | 3, 8 |
| has runtime state worth inspecting | a debugger window | 6 |
| will be used or configured by agents | `<Package>AgentTools` and SKILLS.md | 8, 10 |

---

## 1. Folder and package layout

```text
Assets/Arosoul/Arosoul-<Package>/
├── package.json              com.arosoul.<package>, "unity": "6000.6", author, license, dependencies
├── LICENSE.txt               copy from another Arosoul package
├── README.md                 for humans: setup, usage, tools, notes
├── SKILLS.md                 for agents: see section 10
├── Runtime/
│   ├── Arosoul.<Package>.asmdef
│   ├── AssemblyInfo.cs       [InternalsVisibleTo] for Editor and Tests assemblies
│   └── <Feature folders>/    e.g. Core/, Data/, Attributes/, Parsing/, Console/
├── Editor/
│   ├── Arosoul.<Package>.Editor.asmdef   (includePlatforms: Editor)
│   ├── <Package>Settings.cs, <Package>AgentTools.cs, bakers, build hooks
│   └── Windows/
├── Prefabs/                  prefabs the menu items instantiate
├── Samples/                  optional, with its own Arosoul.<Package>.Samples asmdef
└── Tests/
    ├── Editor/   Arosoul.<Package>.Tests.Editor.asmdef   (Editor only)
    └── PlayMode/ Arosoul.<Package>.Tests.PlayMode.asmdef
```

- **Namespaces:** `Arosoul.<Package>` for runtime, `Arosoul.<Package>.Editor` for editor, `Arosoul.<Package>.Tests` for tests.
  Generated game-facing code goes in the global namespace (section 4).
- **Test asmdefs:** reference `UnityEngine.TestRunner`, `UnityEditor.TestRunner` and the package assemblies. Use `overrideReferences: true`
  with `nunit.framework.dll`, `autoReferenced: false` and `defineConstraints: ["UNITY_INCLUDE_TESTS"]` (copy from Arosoul-Commands).
- **Internals:** keep implementation types `internal`, and expose them to the Editor and Test assemblies through `AssemblyInfo.cs`.
  The public surface is the API facade, the attributes, the extension base classes and the agent tools.
- **Dependencies:** depend on other Arosoul packages only when the feature requires it. Prefer an optional integration over a hard
  dependency, e.g. a separate assembly with `defineConstraints`/`versionDefines`, or an interface the other package implements.

---

## 2. Where data lives

| Data | Store it in | Example |
|---|---|---|
| Project-wide editor settings, committed and shared by the team | `ScriptableSingleton` → `ProjectSettings/<Package>Settings.asset` | `CommandsSettings`, `ScenesSettings` |
| Runtime copy of those settings | baked `ScriptableObject` → `Assets/Generated/<Package>/`, in Preloaded Assets | `CommandsRuntimeSettings`, `SceneCatalog` |
| Designer-authored content items | `ScriptableObject` assets anywhere in `Assets`, collected into a generated registry | Transition definitions → `TransitionRegistry` |
| Code that refers to content by name | generated `.cs` in `Assets/Generated/<Package>/` | `SceneIds.cs`, `TransitionIds.cs`, `GameActions.cs` |
| Per-user editor state (foldouts, last tab, "shown once" flags) | `EditorPrefs` with key `Arosoul.<Package>.<Name>`, or `SessionState` for one session | Transitions' first-run flag |
| Small per-player runtime preferences | `PlayerPrefs` with key `Arosoul.<Package>.<Name>` | console history |
| Larger per-player data (saves, profiles) | files under `Application.persistentDataPath` (or the save system, if there is one) | |
| Per-scene or per-object configuration | serialized fields on components and prefabs | `CommandConsole._openOnStart` |

Never commit per-user state to `ProjectSettings/`, and never put team settings in `EditorPrefs`.

### 2.1 Project settings (ScriptableSingleton)

```csharp
[FilePath("ProjectSettings/<Package>Settings.asset", FilePathAttribute.Location.ProjectFolder)]
public sealed class <Package>Settings : ScriptableSingleton<<Package>Settings>
{
    [SerializeField] <Package>SettingsData _data = new();

    public <Package>SettingsData Data => _data;

    public void SaveSettings() => Save(true);

    /// <summary>The singleton is HideAndDontSave (NotEditable); clear that before binding it to UI.</summary>
    internal SerializedObject CreateSerializedObject()
    {
        hideFlags &= ~HideFlags.NotEditable;
        return new SerializedObject(this);
    }
}
```

- Put the values in a plain `[Serializable] <Package>SettingsData` class with public PascalCase fields, `[Header]` groups,
  full-sentence `[Tooltip]`s and `[Range]` where it fits. The same class is serialized by the editor singleton and the runtime asset,
  and the window draws it with `PropertyField`s.
- Give it `public <Package>SettingsData Clone() => (…)MemberwiseClone();` as long as all fields are values or asset references.
- Asset references (PanelSettings, themes) are fine in the data class. Compare copies with `EditorJsonUtility`, not `JsonUtility`.
- Scenes stores scene references as GUID strings (`SceneRef`) so moving or renaming a scene doesn't break the reference.
  Do the same for any asset type that can't be a direct reference.

### 2.2 Runtime copy of settings

ScriptableSingleton is editor-only. When the runtime needs the settings, bake them into a ScriptableObject:

```csharp
public sealed class <Package>RuntimeSettings : ScriptableObject
{
    [SerializeField] <Package>SettingsData _data = new();
    private static <Package>RuntimeSettings _instance;
    private static <Package>SettingsData _defaults;

    public static <Package>RuntimeSettings Instance => _instance != null ? _instance : _instance = Find();
    public static <Package>SettingsData CurrentData => Instance != null ? Instance.Data : _defaults ??= new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private static <Package>RuntimeSettings Find()
    {
#if UNITY_EDITOR
        // AssetDatabase.FindAssets($"t:{nameof(<Package>RuntimeSettings)}") → load the first one
#else
        // Resources.FindObjectsOfTypeAll<<Package>RuntimeSettings>() → it is loaded because it is preloaded
#endif
    }
}
```

- Path: `Assets/Generated/<Package>/<Package>RuntimeSettings.asset` (or `…Catalog.asset` / `…Registry.asset` for content).
- Add it to `PlayerSettings` preloaded assets, so builds load it without `Resources/` or Addressables.
- Fall back to defaults when it is missing, so the runtime never null-refs on a fresh clone.
- Gate features per build type with a setting and a runtime check (see `CommandsAvailability`: EditorOnly / DevelopmentBuilds / AllBuilds,
  checked with `Application.isEditor` and `Debug.isDebugBuild`).

### 2.3 Content assets and registries

When designers create many items, each item is its own ScriptableObject asset in any folder they choose.
An editor builder finds them with `AssetDatabase.FindAssets("t:<Type>")`, assigns stable ids, and writes:

- one generated registry asset (preloaded) for runtime lookup by id, and
- generated id constants (section 4).

Store the id in the item asset as a stable `int`, assigned once and never reused. Keep a separate renamable `_key` for the
generated member name. Renaming a key then changes code, but never breaks serialized references.

---

## 3. Baking

The baker writes runtime assets from editor data.

- **When to bake:**
  - on every settings change, debounced through `EditorApplication.delayCall`;
  - in an `IPreprocessBuildWithReport`;
  - once on editor load if the asset is missing (`[InitializeOnLoad]` + `delayCall`, skipped while entering play mode);
  - from an `AssetPostprocessor` when relevant assets are added, moved or deleted (Scenes rebakes on `.unity` changes).
- **Write only on change.** Compare before writing (`EditorJsonUtility` for data, `WriteIfChanged` for text) so baking doesn't dirty
  files, cause git noise or trigger recompiles.
- **Not in play mode.** If a bake is requested while playing, validate only and set a pending flag; bake when play mode ends.
- **Validate while baking.** The build preprocessor bakes, validates, and throws `BuildFailedException` listing every error.
  Warnings go to the log only.
- **Keep it preloaded.** Re-add the asset to Preloaded Assets on every bake, without removing the user's other entries.
- **Fill sensible defaults.** E.g. find `UnityDefaultRuntimeTheme` when the theme is empty, and save it back to the settings.
- Expose a `Tools > Arosoul > <Package> > Rebuild …` menu item, or a button in the window, for a manual rebake.

Never hand-edit baked or generated files, and say so in README and SKILLS.md.

---

## 4. Generated code

Use it when game code should refer to content by name with compile-time safety (`SceneIds.Gameplay`, `TransitionIds.Fade`).

- **Location:** `Assets/Generated/<Package>/<Thing>Ids.cs`, next to the baked assets.
- **Namespace:** the global namespace, so game code needs no `using`. The id *types* (`SlotId`, `TransitionId`) stay in the package namespace.
- **Assembly:** no asmdef in `Assets/Generated/<Package>`, so the file compiles into Assembly-CSharp. Document this: projects with their
  own asmdefs must add one there that references the package's runtime assembly.
- **Content:** a header comment saying the file is auto-generated, then one static class of `public static readonly` or `const` members.
  Sanitize identifiers (valid C#, PascalCase, no keywords) and make duplicates unique with numeric suffixes; warn about the collision
  in `Validate()`. Use Allman style, as for hand-written code.
- **Write only if changed**, so an unchanged file never triggers a domain reload.
- **Out-of-sync detection:** keep a function that builds the source in memory and compares it with the file (`IsIdsOutOfSync`).
  Show it in `Validate()` and highlight the regenerate button in the window.
- **Compilation safety:** don't generate while compiling unless the caller opts in (`RefreshIgnoringCompilation`). When a stale
  generated file breaks compilation, the generator can't run; document the recovery (delete or hand-fix the file, then regenerate).
- Regenerate automatically from agent tools after every change (they should never leave ids stale). In the UI, a button is fine if
  regeneration is expensive.

---

## 5. Runtime entry points

### 5.1 API facade

Give the package one static `<Package>API` class as the main way to use it (`SceneAPI`, `TransitionAPI`, `CommandAPI`).
It hides managers and registries, and offers fluent builders where operations have options
(`SceneAPI.Load(...).WithTransition(...).PerformAsync()`). Use `Awaitable` for async operations.

Behind it, prefer instance classes that can be built in isolation (`new CommandRegistry(types)`, `new CommandExecutor(registry, log)`)
with a static `Default`. That way tests don't depend on global state.

### 5.2 Scene objects

When the package needs a scene object:

- **Prefab + hierarchy menu** (the user prefers this for things placed deliberately, like the Commands Console):
  - Ship `Prefabs/<Name>.prefab`.
  - Add `[MenuItem("GameObject/Arosoul/<Name>", false, 10)]` that:
    - selects the existing instance if there already is one;
    - otherwise finds the prefab by name and component (`AssetDatabase.FindAssets("\"<Name>\" t:Prefab")`, then `TryGetComponent`);
    - instantiates it with `PrefabUtility.InstantiatePrefab`;
    - falls back to `new GameObject(name, typeof(T))`;
    - registers the creation with `Undo` and selects the new object.
  - Expose the same function in the agent tools.
- **Auto-created** (for services that should always exist, like the Transitions overlay): create a hidden or `DontDestroyOnLoad`
  object lazily, or from `[RuntimeInitializeOnLoadMethod]`.
- **Singleton rules:** a static `Instance`. A second instance destroys itself in `Awake`. Guard `OnEnable`/`OnDisable` with
  `Instance == this`, because `Destroy` is deferred and they still run. Offer `DontDestroyOnLoad` as a setting (move to the root first).
- **Static reset:** every static field gets reset in a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`
  method, so the package works with domain reload disabled.

### 5.3 Runtime UI (UI Toolkit)

- Create a child `UIDocument` at runtime. Create the GameObject inactive, assign `panelSettings`, then activate it, so the root is
  built with the right settings.
- Let users supply their own `PanelSettings`. Otherwise create one (`ScaleWithScreenSize`, 1920x1080, match 0.5) and assign a
  `ThemeStyleSheet`: without a theme, text doesn't render in builds.
- **Sorting orders:** Transitions overlay 32000, Commands console 32100. Pick a value that fits between them and document it.
- Set the document root to `PickingMode.Ignore` so a full-screen overlay doesn't eat clicks.
- Style in code when the user doesn't need to restyle it (Commands). Use UXML/USS when designers should restyle it.

---

## 6. Editor windows and menus

- **Menu paths:** every Arosoul menu item goes under `Tools/Arosoul/<Package>/<Item>` (`Tools/Arosoul/Scenes/Scene Config`,
  `Tools/Arosoul/Commands/Settings`). This keeps the premade toolkit apart from game-specific tools.
  - **System packages sit above the divider.** Unity draws a divider between neighbouring items whose priorities differ by more than 10,
    and sorts submenus by their lowest priority. Every system item therefore gets an explicit priority in the 100–199 range:
    packages in alphabetical order, about two apart, settings first in each package. Current values: Commands 100–101,
    GameInput 102–103, PlayerSettings 104, Scenes 106–108, Transitions 110–111. Keep the gap between neighbours at 10 or less.
  - **General tools sit below the divider.** `Tools/Arosoul/Utilities/` and `Tools/Arosoul/Shortcuts/` keep the default priority (1000).
  - **Standalone tools** that aren't part of the toolkit's runtime systems (the Project Wizard) stay directly under `Tools/`.
  - **Hierarchy creators** go under `GameObject/Arosoul/<Name>`.
  - **Asset creation** (`[CreateAssetMenu]`) uses `menuName = "Arosoul/<Package>/<Item>"`, e.g. `Arosoul/Transitions/Fade` or
    `Arosoul/ContextFramework/Variables/Int`.
  - Package names in menus are the folder name without `Arosoul-` (`GameInput`, `PlayerSettings`, `ContextFramework`).
  - Messages and docs that name a menu use the same path (`Tools > Arosoul > Scenes > Scene Config`, `Create > Arosoul > PlayerSettings > Settings Layout`).
- **UI Toolkit** for every new window (`CreateGUI`).
- **Settings window:**
  - Bind the singleton's `SerializedObject` (see 2.1) and draw each child of `_data` with a `PropertyField`.
  - Call `TrackSerializedObjectValue`, then save and bake in a debounced commit.
  - Show `Validate()` results as HelpBoxes at the bottom.
  - Add action buttons at the top, such as "Add … To Scene" and "Open Debugger".
  - A `RefreshOpenWindows()` hook lets agent tools rebuild open windows after changing settings from code.
- **Debugger or inspector window:** a searchable `TreeView` grouped by owner (class, slot, …), a details pane, a summary line with
  counts, and issues as clickable HelpBoxes. Double-click opens the source (`AssetDatabase.OpenAsset(script, line)`; see
  `CommandSourceLocator` for finding the line). For live state, refresh on events or a schedule only while playing.
- **Undo:** register undo for scene and asset changes the user makes (`Undo.RegisterCreatedObjectUndo`, `ScenesUndo`). Agent APIs may
  write without undo.

---

## 7. Extensibility

- **Attributes on user code** (`[Command]`, `[CommandPrefix]`): discover them by reflection.
  - Scan only assemblies that reference the package (plus the package itself), via `CurrentAssemblies.GetLoadedAssemblies()`.
  - Skip assemblies that reference `nunit.framework`, so test fixtures never leak into the project.
  - Scan lazily, on first use, and cache per domain.
  - `TypeCache` is faster in the editor, but only covers types, methods and fields.
- **Validate discovered members** and report problems as issues (ERROR = not registered, WARNING = registered with limits) instead of
  throwing. Include the member path and how to fix it in the message.
- **User extension types** (`ValueParser<T>`, `TransitionDefinition` subclasses): make them abstract generic base classes, and register
  non-abstract subclasses with a parameterless constructor automatically. Offer a manual `Register` for tests.
- **Swappable backends** (`ISceneLoader` + `SceneAPI.SetLoader`): use them where tests or alternative sources (Addressables) need a different
  implementation.
- **Stripping:** attributes that mark reflected members can derive from `UnityEngine.Scripting.PreserveAttribute`. Document `[Preserve]` or
  `link.xml` for High stripping.
- **Use current Unity 6.6 APIs:**
  - `FindAnyObjectByType` and `FindObjectsByType(type, FindObjectsInactive)`: sort modes and `FindFirstObjectByType` are deprecated.
  - `CurrentAssemblies.GetLoadedAssemblies()` instead of `AppDomain.GetAssemblies()`.
  - Check the console for `CS0618` deprecation warnings after compiling.

---

## 8. Diagnostics and agent tools

Packages with configuration or authoring get a `public static class <Package>AgentTools` in the Editor assembly:

| Member | Contract |
|---|---|
| `string[] Validate()` | Each entry starts with `ERROR:` (must fix) or `WARNING:` (advisory) and says how to fix it, e.g. "Run <Package>AgentTools.Bake()". Empty means valid. The settings window and the build preprocessor use the same checks. |
| `string Describe()` | Readable text summary of settings and content (one line per item: key, id, type, important values, path) that agents can read instead of opening windows. |
| Mutators (`Add…`, `Remove…`, `Set…`, `ModifySettings(Action<Data>)`) | Do everything the UI can. Save, bake and regenerate before returning, refresh open windows, and throw `ArgumentException` with the valid options for bad input. |
| `Find…`, `Execute…`, `AddXToActiveScene()` | Read-only queries and actions that are useful to automate. |

Runtime code reports problems without throwing in the game loop: log an actionable `Debug.LogError`, or return false and record the error
(`SceneOperation.Error`, the `CommandLog`). Use `Debug.Assert` for required references during initialization.

---

## 9. Testing

- **Edit mode:** pure logic (parsers, planners, graphs, validators, ranking), discovery against fixture types, generators (output and
  out-of-sync detection), bakers (in sync and preloaded), and the agent tools (`Validate()` has no errors on the project).
- **Play mode:** managers and components with fakes (`FakeSceneLoader`), singleton and duplicate handling, timing and pause, and log integration.
- Build isolated instances in tests instead of using `Default`. Reset static fixture state in `[SetUp]`. Use `DestroyImmediate` in
  `[TearDown]` for objects that hold singletons.
- Tests that change project settings or assets must restore them.

---

## 10. Documentation

- **README.md** (humans): one-line summary; Setup; Use (short code samples, an input/syntax table if relevant); Tools (menu paths);
  Notes and limits; Tests.
- **SKILLS.md** (agents):
  - a header with version, Unity version, assemblies/namespaces, dependencies and the automation entry point;
  - then sections for:
    - mental model and data flow;
    - settings and generated files (a table of paths, with "never hand-edit");
    - API usage;
    - the agent tools table and a typical agent loop;
    - rules and gotchas;
    - extending and testing;
    - file map;
    - code conventions.
- XML `/// <summary>` docs on public types and on members whose behaviour isn't obvious. Escape generics in XML (`ValueParser&lt;T&gt;`).

---

## 11. New package checklist

1. Decide which patterns apply (section 0), and write down the data locations (section 2) before writing code.
2. Scaffold the folders, `package.json`, `LICENSE.txt` and asmdefs, including the test asmdefs (section 1).
3. Write the runtime core as instance classes with a static facade, and reset statics on `SubsystemRegistration`.
4. Add editor settings, the baker, build hooks and generated files only if needed.
5. Add windows, the `GameObject/Arosoul/…` creator and agent tools as needed.
6. Write edit mode and play mode tests, then compile with zero errors and no new warnings.
7. Run the tests and smoke-test in Play Mode.
8. Write README.md and SKILLS.md, and remove temporary scripts or screenshots.
