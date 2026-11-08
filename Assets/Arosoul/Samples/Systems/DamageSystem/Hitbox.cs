using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour, IHittable {
    [SerializeField] private float _hitBufferTime = 1;
    [SerializeField] private bool _canHit = true;

    [Header("References")]
    [SerializeField] Health _health;

    private Collider2D _collider;


    private float _hitBufferTimer;
    private bool _hasTriggeredHitBufferEndEvent = true;

    public bool CanHit => _canHit && _hitBufferTimer <= 0;
    public float HitBufferTime => _hitBufferTime;



    /// <summary>
    /// Object: Object that triggered hit event.
    /// Int: Amount transfered on hit.
    /// </summary>
    public event Action<UnityEngine.Object, int> OnHitEvent;
    public event Action<float> OnHitBufferStartEvent; // Float: Buffer time
    public event Action OnHitBufferEndEvent;


    void Awake() {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    void Update() {
        // End hit buffer
        _hitBufferTimer -= Time.deltaTime;
        if (_hitBufferTimer <= 0 && !_hasTriggeredHitBufferEndEvent) {
            _hasTriggeredHitBufferEndEvent = true;
            OnHitBufferEndEvent?.Invoke();
        }
    }

    public void SetActive(bool active) {
        gameObject.SetActive(active);
    }

    public void StartHitBuffer(float? customHitBuffer = null) {
        _hitBufferTimer = customHitBuffer == null ? _hitBufferTime : (float)customHitBuffer;
        _hasTriggeredHitBufferEndEvent = false;
        OnHitBufferStartEvent?.Invoke(_hitBufferTime);
    }

    public void OnHit(UnityEngine.Object sender, int amount) {
        if (!CanHit) return;

        _health.Damage(amount);
        OnHitEvent?.Invoke(sender, amount);

        // Start hit buffer
        StartHitBuffer();
    }


}