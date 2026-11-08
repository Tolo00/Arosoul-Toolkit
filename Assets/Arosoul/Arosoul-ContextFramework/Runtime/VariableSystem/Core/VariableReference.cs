using System;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    /// <summary>
    /// A serializable wrapper that can reference either a constant value
    /// or a Variable asset derived from <see cref="VariableBase{T}"/>.
    /// 
    /// Enables flexible configuration in the Inspector — you can choose
    /// between using a fixed constant or a shared ScriptableObject variable
    /// without changing code.
    /// 
    /// Includes common built-in reference types (FloatReference, IntReference, etc.) 
    /// for simplified usage.
    /// </summary>
    [Serializable]
    public class VariableReference<T, V> where V : VariableBase<T> {
        public bool UseConstant = true;
        public T ConstantValue;
        public V Variable;

        public VariableReference() { }

        public VariableReference(T value) {
            UseConstant = true;
            ConstantValue = value;
        }

        public T Value {
            get { return UseConstant ? ConstantValue : Variable.Value; }
            set { 
                if (UseConstant) ConstantValue = value;
                else Variable.Value = value;
            }
        }
    }    
}
