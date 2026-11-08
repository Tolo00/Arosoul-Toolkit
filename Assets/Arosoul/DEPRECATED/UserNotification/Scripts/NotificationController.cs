using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationController : MonoBehaviour {    
    [SerializeField, Tooltip("Timing logic is independent of time-scale.")] 
    bool _realtime = true;

    [Header("References")]
    [SerializeField] GameObject _imageContainer;
    [SerializeField] Image _imageElement;
    [Space]
    [SerializeField] RectTransform _textContainer;
    [SerializeField] TMP_Text _textElement;
    [Space]
    [SerializeField] CanvasGroup _canvasGroup;

    float _fadeOutDuration;
    private bool _fadingOut;

    float _aliveDuration;
    private float _aliveTimer;

    public EventHandler OnFadeOutComplete;

    void Update()
    {
        if (_fadingOut) return;

        _aliveTimer += _realtime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_aliveTimer > _aliveDuration) FadeOut(); 
    }

    public void SetSymbol(Sprite sprite) {
        _imageElement.sprite = sprite;

        // Remove symbol
        if (sprite == null)
        {
            _imageContainer.SetActive(false);

            Vector2 min = _textContainer.offsetMin;
            min.x = 0;
            _textContainer.offsetMin = min;
        }
    }
    public void SetContent(string text) => _textElement.text = text;
    public void SetAliveDuration(float duration) => _aliveDuration = duration;
    public void SetFadeOutDuration(float duration) => _fadeOutDuration = duration;

    public void FadeOut() => StartCoroutine(FadeOutCoroutine());
    IEnumerator FadeOutCoroutine()
    {
        _fadingOut = true;
        float elapsed = 0f;
        float startAlpha = _canvasGroup.alpha;
        
        while (elapsed < _fadeOutDuration)
        {
            elapsed += _realtime ? Time.unscaledDeltaTime : Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeOutDuration);
            yield return null;
        }
        
        _canvasGroup.alpha = 0f;
        OnFadeOutComplete?.Invoke(this, EventArgs.Empty);
        Destroy(gameObject);
    }
}