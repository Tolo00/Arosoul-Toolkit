using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Base class for all transitions. A transition is a ScriptableObject that holds its settings and knows how to
    /// build and animate its visuals using UI Toolkit.
    ///
    /// To add a new transition:
    /// 1. Create a class that derives from this one and implement <see cref="Build"/> and <see cref="Apply"/>.
    /// 2. Create an asset of it (Create > Arosoul > Transitions, or through Tools > Transition System > Transitions).
    /// 3. A type-safe id is generated automatically (TransitionIds.YourKey).
    /// </summary>
    public abstract class TransitionDefinition : ScriptableObject
    {
        // Assigned by the editor. Stable for the lifetime of the asset.
        [SerializeField, HideInInspector] private int _id;

        [SerializeField, Tooltip("Name of the generated constant. Defaults to the asset name.")]
        private string _key;

        [Header("Defaults")]
        [SerializeField, Min(0f)] private float _defaultInTime = 0.5f;
        [SerializeField, Min(0f)] private float _defaultMinHoldTime = 0.2f;
        [SerializeField, Min(0f)] private float _defaultOutTime = 0.5f;

        [SerializeField, Tooltip("Easing applied to the In and Out phases.")]
        private AnimationCurve _curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField, Tooltip("Block pointer input and notify input systems while this transition is active.")]
        private bool _blockInput = true;

        public TransitionId Id => new TransitionId(_id);
        public string Key => string.IsNullOrEmpty(_key) ? name : _key;
        public float DefaultInTime => _defaultInTime;
        public float DefaultMinHoldTime => _defaultMinHoldTime;
        public float DefaultOutTime => _defaultOutTime;
        public bool BlockInput => _blockInput;

        /// <summary>
        /// Create the visual tree for this transition. Called every time the transition is played.
        /// The returned element is stretched to fill the screen by the player.
        /// </summary>
        public abstract VisualElement Build();

        /// <summary>
        /// Apply the visual state for a phase.
        /// </summary>
        /// <param name="phase">In, Hold or Out.</param>
        /// <param name="t">Progress of the phase from 0 to 1. Eased for In and Out.</param>
        public abstract void Apply(VisualElement visual, TransitionPhase phase, float t);

        /// <summary>
        /// Called while the transition is held, when the progress of the operation behind it changes.
        /// Override to display a progress bar or message.
        /// </summary>
        public virtual void OnProgress(VisualElement visual, float progress, string message) { }

        /// <summary>
        /// Plays the In or Out phase. The default implementation drives <see cref="Apply"/> over time.
        /// Override it to use a custom animation library. The hold phase is always handled by the player.
        /// </summary>
        public virtual async Awaitable PlayAsync(VisualElement visual, TransitionPhase phase, float duration, CancellationToken cancellationToken)
        {
            Apply(visual, phase, 0f);
            if (duration <= 0f)
            {
                Apply(visual, phase, 1f);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
                elapsed += Time.unscaledDeltaTime;
                Apply(visual, phase, Evaluate(Mathf.Clamp01(elapsed / duration)));
            }
            Apply(visual, phase, 1f);
        }

        /// <summary>
        /// Evaluate the easing curve.
        /// </summary>
        public float Evaluate(float t)
        {
            return _curve == null || _curve.length == 0 ? t : _curve.Evaluate(t);
        }

        /// <summary>
        /// Make an element fill its parent. Use inside <see cref="Build"/>.
        /// </summary>
        protected static void StretchToFill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }
    }
}
