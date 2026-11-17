# Arosoul Player Settings (UI Toolkit)

Drop the folder into `Assets/` (or a package). Needs the UI Toolkit runtime (built in). URP is optional.

## Quick start
1. **Custom settings:** `Tools > Arosoul > Player Settings > Custom Settings`. Add rows, press Save.
   This writes `Assets/Settings/CustomSettings.cs`. Use it from code:
   ```csharp
   float sens = CustomSettings.MouseSensitivity;      // read
   CustomSettings.MouseSensitivity = 0.7f;            // set + apply + persist
   var mode = CustomSettings.AimAssist;               // enum settings become a real C# enum (AimAssistOption)
   GameSettings.Changed += id => { ... };             // react to changes
   ```
2. **Layout:** `Create > Arosoul > Player Settings > Settings Layout`. Pick tab orientation / apply mode in the inspector,
   press **Edit Categories**, add categories, toggle settings on and choose a display type for each.
3. **Menu:** add a `Panel Renderer` (with a Panel Settings asset, Source Asset left empty) to a GameObject, add `SettingsMenu`, assign the layout.
   `Open()`, `Close()`, `Toggle()` are public. Switching tab or closing with unapplied edits asks whether to apply or discard them. Calling `Close()` again while the prompt is open dismisses it and keeps the menu open.
4. **Styling (optional):** `Create > Arosoul > Player Settings > Settings Theme`, assign it on the layout.
   Any template or style sheet you leave empty falls back to `Runtime/Resources/PlayerSettings/*`.
   The comment at the top of each UXML lists the element names the code expects (`label`, `slider`, `decrease`, ...).

## Extending
* **New display type:** subclass `SettingDisplay` (id, name, `Supports(type)`, `Bind`), add a `Display_{id}.uxml`.
  It appears in the layout editor automatically. Keep the class stateless.
* **New built-in setting with custom apply logic:** subclass `SettingDefinition` (public, parameterless ctor).
  Found automatically, no registration. See `BuiltIn/` (window logic lives in `WindowState`, brightness in `Integrations/URP`).
* **Different persistence:** implement `ISettingsStorage`, call `GameSettings.Initialize(storage)`.
* If your subclasses live in another assembly, add it to `link.xml` (IL2CPP stripping) and make it reference `Arosoul.PlayerSettings`.

## Behaviour notes
* Apply mode and the Reset button are set per category. In a manual category, edits are staged and Apply / Undo / Reset only touch that category's settings (Reset stages the defaults, press Apply). In an instant category every change applies immediately and Reset applies immediately.
* Custom setting ids are the API names, so renaming one resets its saved value. Min/Max always clamp int and float settings.
* Layout entries render in the order shown in the layout editor (drag to reorder). The same setting can be added more than once to a category.
* Brightness needs `com.unity.render-pipelines.universal` (version define in the URP asmdef). Verify the asmdef assembly names if your 6.6 URP differs.
* Music/SFX volume are not built in because they need your AudioMixer: make them custom float settings and read them from your audio code.
