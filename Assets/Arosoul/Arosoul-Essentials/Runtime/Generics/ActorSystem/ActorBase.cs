using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Essentials.ActorSystem 
{
    /// <summary>
    /// Base MonoBehaviour for actor systems. Manages lifecycle and routing for all registered components.
    /// </summary>
    public abstract class ActorBase<TSelf, TContext, TComponent> : MonoBehaviour
        where TSelf : ActorBase<TSelf, TContext, TComponent>
        where TContext : ActorContext<TSelf>
        where TComponent : ActorComponentBase<TContext>
    {
        [field: SerializeField]
        public TContext Context { get; private set; }

        protected List<TComponent> Components { get; } = new();
        private Dictionary<Type, TComponent> _componentLookup = new();

        public void SetInputEnabled(bool enabled) => Context.SetInputEnabled(enabled);

    #region Component Lifetime

        protected virtual void Awake()
        {
            Context.EntityComponent = (TSelf)this;
            Initialize();
            RegisterComponents();
            InitializeComponents();
        }

        protected virtual void Start()     => DoForAllComponents(c => c.OnReady());
        protected virtual void OnDestroy() {
            CleanupComponents();
            Cleanup();
        }


        /// <summary>
        /// Override to assign component references to the context and populate the Components list.
        /// </summary>
        protected abstract void RegisterComponents();

        protected void RegisterComponent<T>(T component) where T : TComponent
        {
            if (component == null)
            {
                Debug.LogWarning("Tried to register component that is null.", this);
                return;
            }
            Components.Add(component);
            _componentLookup[typeof(T)] = component;
            return;
        }

        public T GetEntityComponent<T>() where T : TComponent
        {
            _componentLookup.TryGetValue(typeof(T), out var component);
            return component as T;
        }


        private void InitializeComponents() => DoForAllComponents(c =>
        {
            if (c == null) Debug.LogWarning("Registered component does not exist.", this);
            c.Initialize(Context);   
        });

        private void CleanupComponents()
        {
            DoForAllComponents(c => { if (c.IsComponentActive) c.Deactivated(); });
            DoForAllComponents(c => c.OnCleanup());
        }

    #endregion

    #region Protected Callbacks

        protected abstract void Initialize();
        protected abstract void Cleanup();

    #endregion

    #region Component Callbacks

        protected virtual void Update()
        {
            DoForAllComponents(component =>
            {
                bool shouldBeActive  = component.ShouldBeActive();

                if (component.IsComponentActive != shouldBeActive)
                    component.SetComponentActive(shouldBeActive);

                if (component.IsComponentActive) component.WhileActive();
                else                             component.WhileInactive();
            });

            DoForAllComponents(c => c.Tick());
        }

        protected virtual void LateUpdate()
        {
            DoForAllComponents(component =>
            {
                component.LateTick();
                if (component.IsComponentActive) component.WhileActiveLate();
                else component.WhileInactiveLate();
            });
        }

        protected virtual void FixedUpdate()
        {
            DoForAllComponents(component =>
            {
                component.FixedTick();
                if (component.IsComponentActive) component.WhileActiveFixed();
                else component.WhileInactiveFixed();
            });
        }


        protected virtual void OnCollisionEnter(Collision collision) => DoForAllComponents(c => c.HandleCollisionEnter(collision));
        protected virtual void OnCollisionExit(Collision collision)  => DoForAllComponents(c => c.HandleCollisionExit(collision));
        protected virtual void OnCollisionStay(Collision collision)  => DoForAllComponents(c => c.HandleCollisionStay(collision));

        protected virtual void OnTriggerEnter(Collider collider) => DoForAllComponents(c => c.HandleTriggerEnter(collider));
        protected virtual void OnTriggerExit(Collider collider)  => DoForAllComponents(c => c.HandleTriggerExit(collider));
        protected virtual void OnTriggerStay(Collider collider)  => DoForAllComponents(c => c.HandleTriggerStay(collider));

        protected virtual void OnCollisionEnter2D(Collision2D collision) => DoForAllComponents(c => c.HandleCollisionEnter2D(collision));
        protected virtual void OnCollisionExit2D(Collision2D collision)  => DoForAllComponents(c => c.HandleCollisionExit2D(collision));
        protected virtual void OnCollisionStay2D(Collision2D collision)  => DoForAllComponents(c => c.HandleCollisionStay2D(collision));

        protected virtual void OnTriggerEnter2D(Collider2D collider) => DoForAllComponents(c => c.HandleTriggerEnter2D(collider));
        protected virtual void OnTriggerExit2D(Collider2D collider)  => DoForAllComponents(c => c.HandleTriggerExit2D(collider));
        protected virtual void OnTriggerStay2D(Collider2D collider)  => DoForAllComponents(c => c.HandleTriggerStay2D(collider));

    #endregion

        protected void DoForAllComponents(Action<TComponent> action)
        {
            foreach (var component in Components) 
            {
                if (component == null)
                {
                    continue;
                }
                action.Invoke(component);
            }
        }
    }
}