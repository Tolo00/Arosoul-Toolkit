using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arosoul.Tools 
{
    public class UIStateColorSync : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Source")]
        [SerializeField] private Selectable sourceSelectable;

        [Header("Targets")]
        [SerializeField] private Graphic[] targets;

        [Header("States to sync")]
        [SerializeField] private bool syncNormal = true;
        [SerializeField] private bool syncSelected = true;
        [SerializeField] private bool syncHighlighted = true;
        [SerializeField] private bool syncPressed = true;
        [SerializeField] private bool syncDisabled = true;

        private Color[] baseColors;
        private bool isPointerOver = false;
        private bool isPointerDown = false;

        private Color? currentColor = null;
        private bool lastInteractable;

        void Awake()
        {
            if (sourceSelectable == null)
                sourceSelectable = GetComponent<Selectable>();

            baseColors = new Color[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    baseColors[i] = targets[i].color;
            }
        }


        void OnEnable()
        {
            lastInteractable = sourceSelectable.IsInteractable();
            UpdateColor();
        }

        void OnDisable() {
            ResetColors();
        }

        void Update()
        {
            if (sourceSelectable == null || EventSystem.current == null)
                return;

            // Track selection state
            bool isSelected = EventSystem.current.currentSelectedGameObject == sourceSelectable.gameObject;

            // Track interactable state
            bool isInteractable = sourceSelectable.IsInteractable();
            if (isInteractable != lastInteractable)
            {
                lastInteractable = isInteractable;
                UpdateColor();
            }

            // Also update for selection changes
            bool shouldSync = (isSelected && syncSelected) || (!isSelected && syncNormal);
            if (shouldSync) UpdateColor();
        }

        private void UpdateColor()
        {
            if (sourceSelectable == null) return;

            print("Update Color");

            Color? newColor = GetColorForCurrentState();

            // Only update if color changed
            if (newColor != currentColor)
            {
                currentColor = newColor;

                for (int i = 0; i < targets.Length; i++)
                {
                    var targetGraphic = targets[i];
                    if (targetGraphic == null) continue;

                    targetGraphic.color = newColor ?? baseColors[i];
                }
            }
        }

        private void ResetColors()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var targetGraphic = targets[i];
                if (targetGraphic == null) continue;

                targetGraphic.color = baseColors[i];
            }
            currentColor = null;
        }

        private Color? GetColorForCurrentState()
        {
            var colors = sourceSelectable.colors;

            if (!sourceSelectable.IsInteractable())
            {
                if (!syncDisabled) return null;
                return colors.disabledColor;
            }
            else if (isPointerDown)
            {
                if (!syncPressed) return null;
                return colors.pressedColor;
            }
            else if (isPointerOver)
            {
                if (!syncHighlighted) return null;
                return colors.highlightedColor;
            }
            else if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == sourceSelectable.gameObject)
            {
                if (!syncSelected) return null;
                return colors.selectedColor;
            }
            else
            {
                if (!syncNormal) return null;
                return colors.normalColor;
            }
        }

        // === Pointer event handlers ===
        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            UpdateColor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            UpdateColor();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            UpdateColor();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            UpdateColor();
        }
    }
}