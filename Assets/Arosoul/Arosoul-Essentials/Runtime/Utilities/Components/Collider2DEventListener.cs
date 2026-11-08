using System;
using UnityEngine;

/// <summary>
/// Attach this to any GameObject to broadcast its collision and trigger events.
/// Other scripts can subscribe to these events to react to its collisions.
/// 
/// NOTE: This component only listens for 2D physics callbacks.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collider2DEventListener : MonoBehaviour {
    Collider2D _collider;

    public bool IsTrigger {
        get { 
            if (_collider == null) SetCollider();
            return _collider.isTrigger; 
        }
        set { 
            if (_collider == null) SetCollider();
            _collider.isTrigger = value; 
        }
    }

    private void SetCollider() {
        _collider = GetComponent<Collider2D>();
    }

    // Collision Events
    public event Action<Collision2D> OnCollisionEnterEvent;
    public event Action<Collision2D> OnCollisionStayEvent;
    public event Action<Collision2D> OnCollisionExitEvent;
    
    // Trigger Events
    public event Action<Collider2D> OnTriggerEnterEvent;
    public event Action<Collider2D> OnTriggerStayEvent;
    public event Action<Collider2D> OnTriggerExitEvent;

    // Collision Callbacks
    private void OnCollisionEnter2D(Collision2D collision) {
        OnCollisionEnterEvent?.Invoke(collision);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        OnCollisionStayEvent?.Invoke(collision);
    }

    private void OnCollisionExit2D(Collision2D collision) {
        OnCollisionExitEvent?.Invoke(collision);
    }

    // Trigger Callbacks
    private void OnTriggerEnter2D(Collider2D other) {
        OnTriggerEnterEvent?.Invoke(other);
    }

    private void OnTriggerStay2D(Collider2D other) {
        OnTriggerStayEvent?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other) {
        OnTriggerExitEvent?.Invoke(other);
    }
}
