using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Plays transitions in an overlay UI Toolkit panel. Created on demand, use <see cref="TransitionAPI"/> to interact with it.
    /// </summary>
    public sealed class TransitionManager : LazyLoaded<TransitionManager>
    {
        private UIDocument _document;
        private VisualElement _root;

        private TransitionHandle _active;
        private VisualElement _visual;
        private IDisposable _inputBlock;

        public TransitionHandle ActiveHandle => _active;

        public static event Action<TransitionHandle> TransitionStarted;
        public static event Action<TransitionHandle> TransitionFinished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearInstance();
            TransitionStarted = null;
            TransitionFinished = null;
        }

        protected override void OnSingletonEnable()
        {
            CreateOverlay();
            InputBlocker.BlockedChanged += InputBlocker_OnBlockedChanged;
        }

        protected override void OnSingletonDisable()
        {
            InputBlocker.BlockedChanged -= InputBlocker_OnBlockedChanged;
        }

        private void CreateOverlay()
        {
            var registry = TransitionRegistry.Instance;
            PanelSettings panelSettings = registry != null ? registry.PanelSettings : null;

            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "[TransitionPanelSettings]";
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
                panelSettings.sortingOrder = registry != null ? registry.SortingOrder : 32000f;

                // Not all Unity versions require a theme, but some log a warning without one.
                var theme = Resources.Load<ThemeStyleSheet>("UnityThemes/UnityDefaultRuntimeTheme");
                if (theme != null) panelSettings.themeStyleSheet = theme;
            }

            // Activate after the panel settings are assigned so the root element is created with them.
            var overlay = new GameObject("[TransitionOverlay]");
            overlay.SetActive(false);
            overlay.transform.SetParent(transform, false);
            _document = overlay.AddComponent<UIDocument>();
            _document.panelSettings = panelSettings;
            overlay.SetActive(true);

            _root = _document.rootVisualElement;
            _root.pickingMode = PickingMode.Ignore;
            _root.style.position = Position.Absolute;
            _root.style.left = 0;
            _root.style.top = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
        }

        // The overlay swallows pointer events while any input block is held, even without an active transition.
        private void InputBlocker_OnBlockedChanged(bool blocked)
        {
            if (_root == null) return;
            _root.pickingMode = blocked ? PickingMode.Position : PickingMode.Ignore;
        }

        internal TransitionHandle Play(TransitionParameters parameters)
        {
            var handle = new TransitionHandle(parameters);

            if (!TransitionRegistry.TryGetDefinition(parameters.Id, out TransitionDefinition definition))
            {
                Debug.LogError($"Unable to find a transition definition for {parameters.Id}. Make sure the id is valid and the registry is up to date (Tools > Transition System > Transitions).", this);
                handle.SetPhase(TransitionPhase.Cancelled);
                handle.DisposeToken();
                return handle;
            }

            if (_active != null)
            {
                if (_active.Parameters.Priority >= parameters.Priority)
                {
                    Debug.LogWarning($"Unable to start transition {parameters.Id}. Its priority ({parameters.Priority}) is not higher than the active transition ({_active.Parameters.Priority}).");
                    handle.SetPhase(TransitionPhase.Cancelled);
                    handle.DisposeToken();
                    return handle;
                }
                CancelActive();
            }

            _active = handle;
            RunAsync(handle, definition);
            return handle;
        }

        private async void RunAsync(TransitionHandle handle, TransitionDefinition definition)
        {
            var token = handle.Token;
            var parameters = handle.Parameters;

            try
            {
                _visual = definition.Build();
                StretchVisual(_visual);
                _root.Add(_visual);

                if (definition.BlockInput)
                    _inputBlock = InputBlocker.Acquire($"Transition: {definition.Key}");

                TransitionStarted?.Invoke(handle);

                // In
                handle.SetPhase(TransitionPhase.In);
                await definition.PlayAsync(_visual, TransitionPhase.In, parameters.InTime, token);

                // Hold
                handle.SetPhase(TransitionPhase.Hold);
                definition.Apply(_visual, TransitionPhase.Hold, 0f);
                float held = 0f;
                while (held < parameters.MinHoldTime || !handle.IsReleased)
                {
                    if (handle.ConsumeProgress(out float progress, out string message))
                        definition.OnProgress(_visual, progress, message);

                    await Awaitable.NextFrameAsync(token);
                    held += Time.unscaledDeltaTime;

                    float t = parameters.MinHoldTime > 0f ? Mathf.Clamp01(held / parameters.MinHoldTime) : 1f;
                    definition.Apply(_visual, TransitionPhase.Hold, t);
                }

                // Out
                handle.SetPhase(TransitionPhase.Out);
                await definition.PlayAsync(_visual, TransitionPhase.Out, parameters.OutTime, token);

                Finish(handle, TransitionPhase.Completed);
            }
            catch (OperationCanceledException)
            {
                Finish(handle, TransitionPhase.Cancelled);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
                Finish(handle, TransitionPhase.Cancelled);
            }
        }

        private static void StretchVisual(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }

        private void CancelActive()
        {
            if (_active == null) return;
            var handle = _active;
            handle.CancelToken();
            Finish(handle, TransitionPhase.Cancelled);
        }

        private void Finish(TransitionHandle handle, TransitionPhase phase)
        {
            if (handle.IsFinished) return;
            handle.SetPhase(phase);

            if (_active == handle)
            {
                if (_visual != null)
                {
                    _visual.RemoveFromHierarchy();
                    _visual = null;
                }

                _inputBlock?.Dispose();
                _inputBlock = null;
                _active = null;
            }

            handle.DisposeToken();
            TransitionFinished?.Invoke(handle);
        }

        internal static void CancelHandle(TransitionHandle handle)
        {
            if (HasInstance && Inst._active == handle)
                Inst.CancelActive();
            else
                handle.SetPhase(TransitionPhase.Cancelled);
        }
    }
}
