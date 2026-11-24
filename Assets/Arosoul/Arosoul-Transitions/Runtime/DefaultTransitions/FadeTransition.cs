using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions
{
    [CreateAssetMenu(menuName = "Arosoul/Transitions/Fade", fileName = "Fade")]
    public class FadeTransition : TransitionDefinition
    {
        [SerializeField] private Color _color = Color.black;

        public override VisualElement Build()
        {
            var element = new VisualElement { name = "fade-transition" };
            StretchToFill(element);
            element.style.backgroundColor = _color;
            element.style.opacity = 0f;
            element.pickingMode = PickingMode.Ignore;
            return element;
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
    }
}
