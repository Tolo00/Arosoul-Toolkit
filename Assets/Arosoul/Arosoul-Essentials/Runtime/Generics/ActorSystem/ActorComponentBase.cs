using UnityEngine;

namespace Arosoul.Essentials.ActorSystem 
{
    /// <summary>
    /// Base class for all components belonging to an actor system.
    /// </summary>
    public abstract class ActorComponentBase<TContext> : MonoBehaviour
        where TContext : class
    {
        public bool IsComponentActive { get; private set; }

        /// <summary>
        /// Should not be called externally.
        /// Logic regarding activation should only be managed through ShouldBeActive() override.
        /// </summary>
        internal void SetComponentActive(bool activated)
        {
            if (activated) Activated();
            else Deactivated();

            IsComponentActive = activated;
        }

        public abstract void Initialize(TContext context); // Awake
        public abstract void OnCleanup();                  // OnDestroy

        /// <summary>
        /// Determines if component should be active.
        /// </summary>
        public abstract bool ShouldBeActive();

        public virtual void OnReady() { }       // Start

        public virtual void Activated() { }     // OnEnable
        public virtual void Deactivated() { }   // OnDisable

        public virtual void WhileActive() { }   // Called in Update() while active.
        public virtual void WhileInactive() { } // Called in Update() while inactive.

        public virtual void WhileActiveFixed() { }
        public virtual void WhileInactiveFixed() { }

        public virtual void WhileActiveLate() { }
        public virtual void WhileInactiveLate() { }

        public virtual void Tick() { }          // Update
        public virtual void LateTick() { }      // LateUpdate
        public virtual void FixedTick() { }     // FixedUpdate

        public virtual void HandleCollisionEnter(Collision collision) { }
        public virtual void HandleCollisionStay(Collision collision) { }
        public virtual void HandleCollisionExit(Collision collision) { }

        public virtual void HandleTriggerEnter(Collider collider) { }
        public virtual void HandleTriggerStay(Collider collider) { }
        public virtual void HandleTriggerExit(Collider collider) { }

        public virtual void HandleCollisionEnter2D(Collision2D collision) { }
        public virtual void HandleCollisionStay2D(Collision2D collision) { }
        public virtual void HandleCollisionExit2D(Collision2D collision) { }

        public virtual void HandleTriggerEnter2D(Collider2D collider) { }
        public virtual void HandleTriggerStay2D(Collider2D collider) { }
        public virtual void HandleTriggerExit2D(Collider2D collider) { }
    }
}