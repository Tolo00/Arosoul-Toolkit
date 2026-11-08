using System;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    [Serializable]
    public class GameObjectReference : VariableReference<GameObject, GameObjectVariable> {
        public GameObjectReference() : base() { }
        public GameObjectReference(GameObject constantValue) : base(constantValue) { }

        public static implicit operator GameObject(GameObjectReference reference) {
            return reference.Value;
        }
    }
}