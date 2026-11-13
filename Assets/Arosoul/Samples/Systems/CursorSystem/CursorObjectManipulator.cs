
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Add this manipulator to a UI element the cursor manager should consider clickable.
/// </summary>
public class CursorObjectManipulator : PointerManipulator, ICursorObject
{
    public bool MarkedClickable => target.enabledInHierarchy && _isClickable;

    private bool _isClickable = true;
    public bool SetClickable(bool clickable) => _isClickable = clickable;

    public CursorObjectManipulator(VisualElement attachedElement)
    {
        target = attachedElement;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        if (CursorManager.Inst != null) CursorManager.Inst.RemoveCursorObject(this);
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        if (CursorManager.Inst != null) CursorManager.Inst.RemoveCursorObject(this);
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (CursorManager.Inst == null)
        {
            Debug.LogWarning("CursorManager instance could not be found.");
            return;
        }

        CursorManager.Inst.AddCursorObject(this);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (CursorManager.Inst == null)
        {
            Debug.LogWarning("CursorManager instance could not be found.");
            return;
        }

        CursorManager.Inst.RemoveCursorObject(this);
    }
}