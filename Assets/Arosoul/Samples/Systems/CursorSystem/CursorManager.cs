using System.Collections.Generic;
using System.Linq;
using Arosoul.Essentials;
using GameInputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CursorSkinEnum { Default, Hover, Press }

[System.Serializable]
public class CursorSkin
{
    public Sprite Sprite;
    public Vector2 Offset;
}

public class CursorManager : SingletonPersistent<CursorManager>
{
    [Header("References")]
    [SerializeField, Required] CursorConfig _config; 
    [SerializeField, Required] RectTransform _cursorTransform;
    [SerializeField, Required] Image _cursorImage;
    
    private Canvas _parentCanvas;
    private RectTransform _canvasRectTransform;
    private Camera _canvasCamera;

    private List<ICursorObject> _activeCursorObjects = new();
    private CursorSkin _currentSkin;
    private CursorSkinEnum _currentSkinEnum;

    void Start()
    {
        _parentCanvas = _cursorTransform.GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            _canvasRectTransform = _parentCanvas.GetComponent<RectTransform>();
            _canvasCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        }

        RefreshCursor();
    }

    void LateUpdate()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame) 
        {
            RefreshCursor();
        }

        SetPointerPosition(Mouse.current.position.value);
    }

    protected override void OnSingletonEnable()
    {
        GameInput.SetMouseVisibilityPreference(false);
    }

    protected override void OnSingletonDisable()
    {
        GameInput.SetMouseVisibilityPreference(true);
    }

    public void AddCursorObject(ICursorObject cursorObject)
    {
        _activeCursorObjects.Add(cursorObject);
        RefreshCursor();
    }

    public void RemoveCursorObject(ICursorObject cursorObject)
    {
        if (!_activeCursorObjects.Contains(cursorObject)) return;
        
        _activeCursorObjects.Remove(cursorObject);
        RefreshCursor();
    }

    private void SetPointerPosition(Vector2 mousePos)
    {
        if (_cursorTransform == null || _canvasRectTransform == null || _currentSkin == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform, 
            mousePos, 
            _canvasCamera, 
            out Vector2 localPoint))
        {
            _cursorTransform.anchoredPosition = localPoint + _currentSkin.Offset;
        }
    }

    public void RefreshCursor()
    {
        if (_currentSkinEnum == CursorSkinEnum.Press && Mouse.current.leftButton.isPressed) return;

        if (_activeCursorObjects.Count == 0 || !_activeCursorObjects.Any(c => c.MarkedClickable)) SetCursorSkin(CursorSkinEnum.Default);
        else
        {
            if (Mouse.current.leftButton.isPressed) SetCursorSkin(CursorSkinEnum.Press);
            else SetCursorSkin(CursorSkinEnum.Hover);
        }
    }

    private void SetCursorSkin(CursorSkinEnum type)
    {
        _currentSkinEnum = type;
        _currentSkin = _config.CursorSkins[type];
        _cursorImage.sprite = _currentSkin.Sprite;
    }
}
