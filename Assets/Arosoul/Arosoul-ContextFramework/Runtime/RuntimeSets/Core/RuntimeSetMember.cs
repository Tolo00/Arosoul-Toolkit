using Arosoul.Essentials;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// Automatically registers and unregisters a GameObject or Component
    /// with a target <see cref="RuntimeSetBase{T}"/> at runtime.
    /// 
    /// Useful for tracking dynamic objects in the scene (such as spawned entities)
    /// without manually managing registration.
    /// 
    /// Supports two modes of registration:
    /// - <see cref="RegistrationMode.OnAwakeDestroy"/>: Register on Awake, remove on Destroy.
    /// - <see cref="RegistrationMode.OnEnableDisable"/>: Register on Enable, remove on Disable.
    /// 
    /// Ensures runtime sets stay accurate and automatically reflect the current scene state.
    /// </summary>
    [DisallowMultipleComponent]
    public class RuntimeSetMember : MonoBehaviour {
        public enum RegistrationMode {
            OnAwakeDestroy,
            OnEnableDisable
        }

        [SerializeField, ForceInterface(typeof(IRuntimeSet))] private Object _runtimeSetObject;
        [SerializeField] private RegistrationMode _registrationMode = RegistrationMode.OnAwakeDestroy;

        private IRuntimeSet _runtimeSetInterface;



        private void AddToSet() {
            if (_runtimeSetInterface == null) {
                Debug.LogWarning("Missing runtime set.", this);
                return;
            }

            System.Type targetType = _runtimeSetInterface.SetType;
            // GameObject special case - Required due to it not being a component
            if (targetType == typeof(GameObject)) {
                _runtimeSetInterface.AddObject(gameObject);
                return;
            }

            // Try find and add component to set
            Component component = GetComponent(targetType);
            if (component != null) {
                _runtimeSetInterface.AddObject(component);
            }
            else {
                Debug.LogError($"GameObject does not contain RuntimeSet type ({targetType.Name})", this);
            }
        }

        private void RemoveFromSet() {
            if (_runtimeSetInterface == null) {
                return;
            }

            System.Type targetType = _runtimeSetInterface.SetType;
            // GameObject special case - Required due to it not being a component
            if (targetType == typeof(GameObject)) {
                _runtimeSetInterface.RemoveObject(gameObject);
                return;
            }

            Component component = GetComponent(targetType);
            if (component != null) {
                _runtimeSetInterface.RemoveObject(component);
            }
            else {
                Debug.LogError($"GameObject does not contain RuntimeSet type ({targetType.Name})", this);
            }
        }


        // On Enable / Disable
        void OnEnable() {
            if (_registrationMode == RegistrationMode.OnEnableDisable)
                AddToSet();
        }

        void OnDisable() {
            if (_registrationMode == RegistrationMode.OnEnableDisable) 
                RemoveFromSet();
        }


        // On Awake / Destroy
        void Awake() {
            // Unity serializes as Object → must go through ScriptableObject or Component first
            if (_runtimeSetObject is ScriptableObject so)
                _runtimeSetInterface = so as IRuntimeSet;
            
            if (_registrationMode == RegistrationMode.OnAwakeDestroy) 
                AddToSet();
        }

        void OnDestroy() {
            if (_registrationMode == RegistrationMode.OnAwakeDestroy) 
                RemoveFromSet();
        }
    }
}