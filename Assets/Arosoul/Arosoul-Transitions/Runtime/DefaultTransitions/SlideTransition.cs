using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions
{
    [CreateAssetMenu(menuName = "Arosoul/Transitions/Slide", fileName = "Slide")]
    public class SlideTransition : TransitionDefinition
    {
        public enum SlideDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        [SerializeField] private Color _color = Color.black;

        [SerializeField, Tooltip("Direction the panel moves while sliding in. It continues in the same direction when sliding out.")]
        private SlideDirection _direction = SlideDirection.Right;

        public override VisualElement Build()
        {
            var element = new VisualElement { name = "slide-transition" };
            StretchToFill(element);
            element.style.backgroundColor = _color;
            element.pickingMode = PickingMode.Ignore;
            SetOffset(element, -DirectionVector());
            return element;
        }

        public override void Apply(VisualElement visual, TransitionPhase phase, float t)
        {
            Vector2 dir = DirectionVector();
            switch (phase)
            {
                case TransitionPhase.In:
                    SetOffset(visual, -dir * (1f - t));
                    break;
                case TransitionPhase.Hold:
                    SetOffset(visual, Vector2.zero);
                    break;
                case TransitionPhase.Out:
                    SetOffset(visual, dir * t);
                    break;
            }
        }

        private Vector2 DirectionVector()
        {
            switch (_direction)
            {
                case SlideDirection.Left: return Vector2.left;
                case SlideDirection.Up: return Vector2.down;   // UI Toolkit y axis points down
                case SlideDirection.Down: return Vector2.up;
                default: return Vector2.right;
            }
        }

        // Offset is expressed in fractions of the element size.
        private static void SetOffset(VisualElement element, Vector2 offset)
        {
            element.style.translate = new Translate(Length.Percent(offset.x * 100f), Length.Percent(offset.y * 100f));
        }
    }
}
