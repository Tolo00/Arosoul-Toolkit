using System;
using UnityEngine;

/// <summary>
/// Attach this to any GameObject to broadcast its collision and trigger events.
/// Other scripts can subscribe to these events to react to its collisions.
/// 
/// NOTE: This component only listens for 3D physics callbacks.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ColliderEventListener : MonoBehaviour {
    Collider _collider;

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
        _collider = GetComponent<Collider>();
    }

    // Collision Events
    public event Action<Collision> OnCollisionEnterEvent;
    public event Action<Collision> OnCollisionStayEvent;
    public event Action<Collision> OnCollisionExitEvent;
    
    // Trigger Events
    public event Action<Collider> OnTriggerEnterEvent;
    public event Action<Collider> OnTriggerStayEvent;
    public event Action<Collider> OnTriggerExitEvent;

    // Collision Callbacks
    private void OnCollisionEnter(Collision collision) {
        OnCollisionEnterEvent?.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision) {
        OnCollisionStayEvent?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision) {
        OnCollisionExitEvent?.Invoke(collision);
    }

    // Trigger Callbacks
    private void OnTriggerEnter(Collider other) {
        OnTriggerEnterEvent?.Invoke(other);
    }

    private void OnTriggerStay(Collider other) {
        OnTriggerStayEvent?.Invoke(other);
    }

    private void OnTriggerExit(Collider other) {
        OnTriggerExitEvent?.Invoke(other);
    }
}
