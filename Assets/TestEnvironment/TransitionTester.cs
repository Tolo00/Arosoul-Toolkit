using System;
using System.Collections.Generic;
using UnityEngine;
using Arosoul.Transitions;


#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

/// <summary>
/// Manual test component for Transitions. Add it to a GameObject in an empty scene, enter play mode and use the inspector buttons.
/// Covers playing, custom times, priority, hold until released, progress reporting, cancelling and input blocking.
/// </summary>
[AddComponentMenu("Arosoul/Transitions/Transition Tester")]
public class TransitionTester : MonoBehaviour
{
    #region Fields

    [Header("Transition")]
    [SerializeField] private TransitionId _transition;
    [SerializeField] private int _priority;
    [SerializeField] private bool _holdUntilReleased;

    [Header("Times (only used when Override Times is on)")]
    [SerializeField] private bool _overrideTimes;
    [SerializeField, Min(0f)] private float _inTime = 0.5f;
    [SerializeField, Min(0f)] private float _minHoldTime = 0.2f;
    [SerializeField, Min(0f)] private float _outTime = 0.5f;

    [Header("Progress")]
    [SerializeField, Min(0.1f)] private float _simulatedLoadDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float _progress = 0.5f;
    [SerializeField] private string _message = "Testing";

    [Header("Debug")]
    [SerializeField] private bool _logPhases = true;

    private readonly List<IDisposable> _blocks = new List<IDisposable>();

    #endregion

    #region Properties

    /// <summary>
    /// Handle of the transition started last by this component.
    /// </summary>
    public TransitionHandle Handle { get; private set; }

    /// <summary>
    /// Number of input blocks currently held by this component.
    /// </summary>
    public int HeldBlockCount => _blocks.Count;

    #endregion

    private void OnDestroy()
    {
        ReleaseAllInputBlocks();
    }

    #region Actions

    /// <summary>
    /// Play the selected transition with the current settings.
    /// </summary>
    public void Play()
    {
        if (!CanPlay()) return;
        Track(TransitionAPI.Play(BuildParameters()));
    }

    /// <summary>
    /// Play held, wait for the Hold phase, report fake loading progress, then release.
    /// </summary>
    public async void PlayWithSimulatedLoading()
    {
        if (!CanPlay()) return;

        TransitionHandle handle = TransitionAPI.Play(BuildParameters().WithHoldUntilReleased());
        Track(handle);
        await handle.WaitForPhaseAsync(TransitionPhase.Hold);

        float elapsed = 0f;
        while (this != null && !handle.IsFinished && elapsed < _simulatedLoadDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _simulatedLoadDuration);
            handle.SetProgress(t, $"Loading {Mathf.RoundToInt(t * 100f)}%");
            await Awaitable.NextFrameAsync();
        }

        if (!handle.IsFinished) handle.Release();
    }

    /// <summary>
    /// Allow the active transition to leave its Hold phase.
    /// </summary>
    public void ReleaseActive()
    {
        TransitionAPI.GetActive()?.Release();
    }

    /// <summary>
    /// Cancel the active transition. Its visuals are removed immediately.
    /// </summary>
    public void CancelActive()
    {
        TransitionAPI.GetActive()?.Cancel();
    }

    /// <summary>
    /// Send the progress and message fields to the active transition.
    /// </summary>
    public void SendProgress()
    {
        TransitionAPI.GetActive()?.SetProgress(_progress, _message);
    }

    /// <summary>
    /// Acquire one more input block. Blocks are reference counted.
    /// </summary>
    public void AcquireInputBlock()
    {
        _blocks.Add(InputBlocker.Acquire($"TransitionTester #{_blocks.Count + 1}"));
    }

    /// <summary>
    /// Release the most recent input block held by this component.
    /// </summary>
    public void ReleaseInputBlock()
    {
        if (_blocks.Count == 0) return;
        _blocks[_blocks.Count - 1].Dispose();
        _blocks.RemoveAt(_blocks.Count - 1);
    }

    private void ReleaseAllInputBlocks()
    {
        for (int i = 0; i < _blocks.Count; i++)
            _blocks[i].Dispose();
        _blocks.Clear();
    }

    #endregion

    #region Helpers

    private bool CanPlay()
    {
        if (!_transition.IsNone) return true;
        Debug.LogWarning("TransitionTester: select a transition first.", this);
        return false;
    }

    private TransitionParameters BuildParameters()
    {
        TransitionParameters parameters = _overrideTimes
            ? new TransitionParameters(_transition, _inTime, _minHoldTime, _outTime)
            : TransitionParameters.Default(_transition);

        parameters = parameters.WithPriority(_priority);
        if (_holdUntilReleased) parameters = parameters.WithHoldUntilReleased();
        return parameters;
    }

    private void Track(TransitionHandle handle)
    {
        Handle = handle;
        if (_logPhases)
            handle.PhaseChanged += phase => Debug.Log($"TransitionTester: {handle.Parameters.Id} -> {phase}");
    }

    #endregion
}

#if UNITY_EDITOR
#region Inspector

// Fully qualified, because the namespace Arosoul.Transitions.Editor would otherwise hide UnityEditor.Editor.
[CustomEditor(typeof(TransitionTester))]
internal class TransitionTesterInspector : UnityEditor.Editor
{
    private readonly List<Button> _buttons = new List<Button>();
    private HelpBox _status;

    public override VisualElement CreateInspectorGUI()
    {
        var tester = (TransitionTester)target;
        var root = new VisualElement();
        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        AddTitle(root, "Playback");
        VisualElement row = AddRow(root);
        AddButton(row, "Play", tester.Play);
        AddButton(row, "Play With Simulated Loading", tester.PlayWithSimulatedLoading);
        row = AddRow(root);
        AddButton(row, "Release", tester.ReleaseActive);
        AddButton(row, "Cancel", tester.CancelActive);
        AddButton(row, "Send Progress", tester.SendProgress);

        AddTitle(root, "Input Blocking");
        row = AddRow(root);
        AddButton(row, "Acquire Block", tester.AcquireInputBlock);
        AddButton(row, "Release Block", tester.ReleaseInputBlock);

        AddTitle(root, "Status");
        _status = new HelpBox("", HelpBoxMessageType.None);
        root.Add(_status);

        root.schedule.Execute(() => Refresh(tester)).Every(100);
        Refresh(tester);
        return root;
    }

    private void Refresh(TransitionTester tester)
    {
        bool playing = Application.isPlaying;
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].SetEnabled(playing);

        _status.text = playing ? BuildStatus(tester) : "Enter play mode to use the buttons.";
    }

    private static string BuildStatus(TransitionTester tester)
    {
        var sb = new StringBuilder();

        TransitionHandle active = TransitionAPI.GetActive();
        if (active != null)
        {
            sb.AppendLine($"Active: {active.Parameters.Id}, phase {active.Phase}, released {active.IsReleased}");
            sb.AppendLine($"Progress: {active.Progress:0.00} \"{active.Message}\"");
        }
        else
        {
            sb.AppendLine("Active: none");
        }

        sb.AppendLine($"Last handle: {(tester.Handle != null ? tester.Handle.Phase.ToString() : "none")}");
        sb.AppendLine($"Input blocked: {InputBlocker.IsBlocked} ({InputBlocker.BlockCount} blocks, {tester.HeldBlockCount} from tester)");

        IReadOnlyList<string> reasons = InputBlocker.GetReasons();
        for (int i = 0; i < reasons.Count; i++)
            sb.AppendLine($"  - {reasons[i]}");

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