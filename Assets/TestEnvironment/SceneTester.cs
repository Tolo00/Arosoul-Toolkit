using System;
using System.Collections.Generic;
using Arosoul.Transitions;
using UnityEngine;
using Arosoul.Scenes;


#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

/// <summary>
/// Manual test component for Scenes. Add it to a GameObject in a scene that is NOT the one you load into the tested slot
/// (for example the core scene), enter play mode and use the inspector buttons.
/// Covers loading, unloading, reloading, dependencies, queueing, cancelling, transitions, payloads and input blocking.
/// </summary>
[AddComponentMenu("Arosoul/Scenes/Scene Tester")]
public class SceneTester : MonoBehaviour
{
    #region Fields

    [Header("Target")]
    [SerializeField] private SlotId _slot;
    [SerializeField] private SceneRef _scene;
    [SerializeField] private bool _setActive = true;

    [Header("Second target (Load Both)")]
    [SerializeField] private SlotId _secondSlot;
    [SerializeField] private SceneRef _secondScene;

    [Header("Operation options")]
    [SerializeField] private bool _useTransition = true;
    [SerializeField] private TransitionId _transition;
    [SerializeField] private bool _blockInput = true;
    [SerializeField] private bool _unloadUnusedAssets;
    [SerializeField, Tooltip("Handed to initializers as a string payload. Empty means no payload.")]
    private string _payload;

    #endregion

    #region Properties

    /// <summary>
    /// Operation performed last by this component.
    /// </summary>
    public SceneOperation LastOperation { get; private set; }

    #endregion

    #region Actions

    /// <summary>
    /// Load the target scene into the target slot.
    /// </summary>
    public void Load()
    {
        if (!HasTarget(_slot, _scene)) return;
        Run(Build().Load(_slot, _scene, _setActive));
    }

    /// <summary>
    /// Unload and load the target scene again.
    /// </summary>
    public void Reload()
    {
        if (!HasTarget(_slot, _scene)) return;
        Run(Build().Load(_slot, _scene, _setActive, reload: true));
    }

    /// <summary>
    /// Unload whatever is in the target slot.
    /// </summary>
    public void Unload()
    {
        if (_slot.IsNone)
        {
            Debug.LogWarning("SceneTester: select a slot first.", this);
            return;
        }
        Run(Build().Unload(_slot));
    }

    /// <summary>
    /// Load the target and the second target in one operation.
    /// </summary>
    public void LoadBoth()
    {
        if (!HasTarget(_slot, _scene) || !HasTarget(_secondSlot, _secondScene)) return;
        Run(Build().Load(_slot, _scene, _setActive).Load(_secondSlot, _secondScene));
    }

    /// <summary>
    /// Queue a load and an unload of the target slot right after each other, to test that operations run in order.
    /// </summary>
    public void QueueLoadThenUnload()
    {
        if (!HasTarget(_slot, _scene)) return;
        Run(Build().Load(_slot, _scene, _setActive));
        Run(Build().Unload(_slot));
    }

    /// <summary>
    /// Cancel the last operation. Only works while it is waiting in the queue.
    /// </summary>
    public void CancelLast()
    {
        if (LastOperation == null) return;
        if (!LastOperation.Cancel())
            Debug.Log("SceneTester: the last operation is already running or finished and can not be cancelled.", this);
    }

    #endregion

    #region Helpers

    private bool HasTarget(SlotId slot, SceneRef scene)
    {
        if (!slot.IsNone && scene.IsValid) return true;
        Debug.LogWarning("SceneTester: select a slot and a scene first.", this);
        return false;
    }

    private SceneOperation Build()
    {
        SceneOperation operation = SceneAPI.NewOperation()
            .WithInputBlocking(_blockInput)
            .WithUnloadUnusedAssets(_unloadUnusedAssets);

        if (_useTransition && !_transition.IsNone) operation.WithTransition(_transition);
        if (!string.IsNullOrEmpty(_payload)) operation.WithPayload(_payload);
        return operation;
    }

    // Does not await, so the component is not needed after the call. The tester scene may be unloaded by the operation.
    private void Run(SceneOperation operation)
    {
        LastOperation = operation;
        operation.Perform();
    }

    #endregion
}

#if UNITY_EDITOR
#region Inspector

// Fully qualified, because the namespace Arosoul.Scenes.Editor would otherwise hide UnityEditor.Editor.
[CustomEditor(typeof(SceneTester))]
internal class SceneTesterInspector : UnityEditor.Editor
{
    private const int LogLines = 8;

    private readonly List<Button> _buttons = new List<Button>();
    private HelpBox _status;

    public override VisualElement CreateInspectorGUI()
    {
        var tester = (SceneTester)target;
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        AddTitle(root, "Operations");
        VisualElement row = AddRow(root);
        AddButton(row, "Load", tester.Load);
        AddButton(row, "Reload", tester.Reload);
        AddButton(row, "Unload", tester.Unload);
        row = AddRow(root);
        AddButton(row, "Load Both", tester.LoadBoth);
        AddButton(row, "Queue Load + Unload", tester.QueueLoadThenUnload);
        AddButton(row, "Cancel Last", tester.CancelLast);

        AddTitle(root, "Status");
        _status = new HelpBox("", HelpBoxMessageType.None);
        root.Add(_status);

        root.schedule.Execute(() => Refresh(tester)).Every(100);
        Refresh(tester);
        return root;
    }

    private void Refresh(SceneTester tester)
    {
        bool playing = Application.isPlaying;
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].SetEnabled(playing);

        _status.text = playing ? BuildStatus(tester) : "Enter play mode to use the buttons.";
    }

    private static string BuildStatus(SceneTester tester)
    {
        var sb = new StringBuilder();

        SceneOperation current = SceneManager.HasInstance ? SceneManager.Inst.CurrentOperation : null;
        int queued = SceneManager.HasInstance ? SceneManager.Inst.QueuedCount : 0;
        sb.AppendLine($"Busy: {SceneAPI.IsBusy} (queued: {queued})");
        if (current != null)
            sb.AppendLine($"Running: {current.Describe()} | {current.Phase} {current.Progress:P0}");

        SceneOperation last = tester.LastOperation;
        if (last != null)
            sb.AppendLine($"Last: {last.State}{(last.Error != null ? " - " + last.Error.Message : "")}");

        TransitionHandle transition = TransitionAPI.GetActive();
        sb.AppendLine($"Transition: {(transition != null ? $"{transition.Parameters.Id} ({transition.Phase})" : "none")}");
        sb.AppendLine($"Input blocked: {InputBlocker.IsBlocked}");

        sb.AppendLine("Loaded scenes:");
        IReadOnlyList<LoadedSceneInfo> loaded = SceneAPI.LoadedScenes;
        if (loaded.Count == 0) sb.AppendLine("  none");
        for (int i = 0; i < loaded.Count; i++)
        {
            LoadedSceneInfo info = loaded[i];
            string tag = info.IsCore ? "core" : info.IsDependencyOnly ? "dependency" : info.Slot.ToString();
            sb.AppendLine($"  {info.Name} [{tag}] load {info.LoadDuration:0.00}s, init {info.InitializationDuration:0.00}s");
        }

        sb.AppendLine("Log:");
        IReadOnlyList<SceneEventLog.Entry> entries = SceneEventLog.Entries;
        for (int i = Mathf.Max(0, entries.Count - LogLines); i < entries.Count; i++)
            sb.AppendLine($"  {entries[i].Time:0.0}  {entries[i].Message}");

        return sb.ToString().TrimEnd();
    }

    #region UI helpers

    private static void AddTitle(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginTop = 8;
        parent.Add(label);
    }

    private static VisualElement AddRow(VisualElement parent)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        parent.Add(row);
        return row;
    }

    private void AddButton(VisualElement parent, string text, Action onClick)
    {
        var button = new Button(onClick) { text = text };
        button.style.flexGrow = 1;
        parent.Add(button);
        _buttons.Add(button);
    }

    #endregion
}

#endregion
#endif