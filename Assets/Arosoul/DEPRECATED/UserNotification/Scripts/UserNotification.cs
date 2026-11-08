using System;
using System.Collections.Generic;
using Arosoul.Essentials;
using UnityEngine;
using UnityEngine.UI;

public enum NotificationType { Info, Warning, Error }

public class UserNotification : SingletonPersistent<UserNotification>
{
    [SerializeField] float _aliveDuration = 3;
    [SerializeField] float _fadeOutDuration = 0.5f;
    [Space]
    [SerializeField, Min(1),Tooltip("Max amount of notifications before the oldest start to manually fade out.")] 
    int _maxCount = 6;

    [Header("References")]
    [SerializeField] GridLayoutGroup _notificationLayout;
    [SerializeField] NotificationController _notificationTemplate;
    [Space]
    [SerializeField] EnumArrayContainer<Sprite, NotificationType> _notificationSymbols;

    private List<NotificationController> _activeControllers = new();

    void Awake()
    {
        _notificationTemplate.gameObject.SetActive(false);
    }

    public void Push(string content, NotificationType type) {
        Inst.HandlePush(content, Inst._notificationSymbols[(int)type], Inst._aliveDuration);
    }
    public void Push(string content, NotificationType type, float aliveDuration) {
        Inst.HandlePush(content, Inst._notificationSymbols[(int)type], aliveDuration);
    }

    
    public void Push(string content, Sprite symbol) {
        Inst.HandlePush(content, symbol, Inst._aliveDuration);
    }
    public void Push(string content, Sprite symbol, float aliveDuration) {
        Inst.HandlePush(content, symbol, aliveDuration);
    }
    

    private void HandlePush(string content, Sprite symbol, float aliveDuration)
    {
        var controller = Instantiate(_notificationTemplate).GetComponent<NotificationController>();
        controller.transform.SetParent(_notificationLayout.transform);
        controller.transform.SetAsFirstSibling();

        controller.GetComponent<RectTransform>().localScale = Vector3.one;
        controller.gameObject.SetActive(true);

        controller.SetSymbol(symbol);
        controller.SetContent(content);
        controller.SetAliveDuration(aliveDuration);
        controller.SetFadeOutDuration(_fadeOutDuration);

        controller.OnFadeOutComplete += NotificationController_OnFadeOutComplete;
        _activeControllers.Add(controller);

        // Prevent too many from existing at once.
        if (_activeControllers.Count > _maxCount)
        {
            _activeControllers[0].FadeOut();
            _activeControllers.RemoveAt(0);
        }
    }

    private void NotificationController_OnFadeOutComplete(object sender, EventArgs args)
    {
        NotificationController controller = sender as NotificationController;
        _activeControllers.Remove(controller);
    }
}


