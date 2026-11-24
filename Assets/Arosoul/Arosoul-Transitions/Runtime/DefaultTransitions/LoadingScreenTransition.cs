using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Fades in a full screen loading screen with a progress bar and an optional random tip.
    /// Progress is fed by TransitionHandle.SetProgress, Scenes does this automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "Arosoul/Transitions/Loading Screen", fileName = "LoadingScreen")]
    public class LoadingScreenTransition : TransitionDefinition
    {
        [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        [SerializeField] private Color _barColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        [SerializeField] private Color _textColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        [SerializeField] private string _defaultMessage = "Loading...";
        [SerializeField] private string[] _tips = System.Array.Empty<string>();

        private sealed class View
        {
            public Label Message;
            public VisualElement Fill;
        }

        public override VisualElement Build()
        {
            var root = new VisualElement { name = "loading-screen-transition" };
            StretchToFill(root);
            root.style.backgroundColor = _backgroundColor;
            root.style.opacity = 0f;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.pickingMode = PickingMode.Ignore;

            var message = new Label(_defaultMessage);
            message.style.color = _textColor;
            message.style.fontSize = 28;
            message.style.marginBottom = 16;
            root.Add(message);

            var track = new VisualElement();
            track.style.width = Length.Percent(40);
            track.style.height = 8;
            track.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
            track.style.borderTopLeftRadius = 4;
            track.style.borderTopRightRadius = 4;
            track.style.borderBottomLeftRadius = 4;
            track.style.borderBottomRightRadius = 4;
            root.Add(track);

            var fill = new VisualElement();
            fill.style.width = Length.Percent(0);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = _barColor;
            fill.style.borderTopLeftRadius = 4;
            fill.style.borderTopRightRadius = 4;
            fill.style.borderBottomLeftRadius = 4;
            fill.style.borderBottomRightRadius = 4;
            track.Add(fill);

            if (_tips != null && _tips.Length > 0)
            {
                var tip = new Label(_tips[Random.Range(0, _tips.Length)]);
                tip.style.color = new Color(_textColor.r, _textColor.g, _textColor.b, 0.6f);
                tip.style.fontSize = 16;
                tip.style.marginTop = 24;
                tip.style.unityTextAlign = TextAnchor.MiddleCenter;
                tip.style.whiteSpace = WhiteSpace.Normal;
                tip.style.maxWidth = Length.Percent(50);
                root.Add(tip);
            }

            root.userData = new View { Message = message, Fill = fill };
            return root;
        }

        public override void Apply(VisualElement visual, TransitionPhase phase, float t)
        {
            switch (phase)
            {
                case TransitionPhase.In:
                    visual.style.opacity = t;
                    break;
                case TransitionPhase.Hold:
                    visual.style.opacity = 1f;
                    break;
                case TransitionPhase.Out:
                    visual.style.opacity = 1f - t;
                    break;
            }
        }

        public override void OnProgress(VisualElement visual, float progress, string message)
        {
            if (!(visual.userData is View view)) return;
            view.Fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
            if (!string.IsNullOrEmpty(message)) view.Message.text = message;
        }
    }
}
