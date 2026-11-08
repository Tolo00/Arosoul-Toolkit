using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageTrigger : MonoBehaviour {
    [SerializeField] private int damageAmount = 1;

    private Collider2D _collider;
    
    public event Action<Collider2D> OnHit;

    void Awake() {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D otherCollider) {
        if (otherCollider.TryGetComponent<IHittable>(out var hittable)) {
            hittable.OnHit(gameObject, damageAmount);
            OnHit?.Invoke(otherCollider);
        }
    }

    public void SetActive(bool active) => gameObject.SetActive(active);
    public void SetPosition(Vector2 position) => transform.position = position; 
    public void SetLocalPosition(Vector2 localPosition) => transform.localPosition = localPosition; 
    public void SetLocalScale(Vector2 size) => transform.localScale = size; 
    public void SetOffset(Vector2 offset) => _collider.offset = offset; 
}