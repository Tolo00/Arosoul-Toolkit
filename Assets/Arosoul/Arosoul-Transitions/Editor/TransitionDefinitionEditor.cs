using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Default inspector for transitions with a live preview.
    /// The preview drives Apply directly, so transitions that override PlayAsync with a custom animation preview their In/Out phases through Apply only.
    /// </summary>
    [CustomEditor(typeof(TransitionDefinition), true)]
    public class TransitionDefinitionEditor : UnityEditor.Editor
    {
        private VisualElement _stage;
        private IVisualElementScheduledItem _previewItem;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var definition = (TransitionDefinition)target;

            var idLabel = new Label(definition.Id.IsNone ? "Id: unassigned" : $"Id: {definition.Id.Value}   (TransitionIds.{definition.Key})");
            idLabel.style.opacity = 0.7f;
            idLabel.style.marginBottom = 6;
            root.Add(idLabel);

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var header = new Label("Preview");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 10;
            root.Add(header);

            _stage = new VisualElement();
            _stage.style.height = 160;
            _stage.style.marginTop = 4;
            _stage.style.backgroundColor = new Color(0.35f, 0.4f, 0.5f);
            _stage.style.overflow = Overflow.Hidden;
            root.Add(_stage);

            var play = new Button(() => Play(definition)) { text = "Play" };
            play.style.marginTop = 4;
            root.Add(play);

            root.RegisterCallback<DetachFromPanelEvent>(_ => _previewItem?.Pause());
            return root;
        }

        private void Play(TransitionDefinition definition)
        {
            _previewItem?.Pause();
            _stage.Clear();

            VisualElement visual = definition.Build();
            visual.style.position = Position.Absolute;
            visual.style.left = 0;
            visual.style.top = 0;
            visual.style.right = 0;
            visual.style.bottom = 0;
            _stage.Add(visual);

            float inTime = definition.DefaultInTime;
            float holdTime = Mathf.Max(definition.DefaultMinHoldTime, 0.5f);
            float outTime = definition.DefaultOutTime;
            double start = EditorApplication.timeSinceStartup;

            _previewItem = _stage.schedule.Execute(() =>
            {
                float elapsed = (float)(EditorApplication.timeSinceStartup - start);

                if (elapsed < inTime)
                {
                    definition.Apply(visual, TransitionPhase.In, definition.Evaluate(elapsed / inTime));
                }
                else if (elapsed < inTime + holdTime)
                {
                    float t = (elapsed - inTime) / holdTime;
                    definition.Apply(visual, TransitionPhase.Hold, t);
                    definition.OnProgress(visual, t, "Preview");
                }
                else if (elapsed < inTime + holdTime + outTime)
                {
                    definition.Apply(visual, TransitionPhase.Out, definition.Evaluate((elapsed - inTime - holdTime) / outTime));
                }
                else
                {
                    definition.Apply(visual, TransitionPhase.Out, 1f);
                    _previewItem.Pause();
                }
            }).Every(16);
        }
    }
}
