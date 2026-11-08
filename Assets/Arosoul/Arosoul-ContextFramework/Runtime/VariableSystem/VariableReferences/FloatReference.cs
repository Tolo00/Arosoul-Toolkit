using System;

namespace Arosoul.ContextFramework 
{
    [Serializable]
    public class FloatReference : VariableReference<float, FloatVariable> {
        public FloatReference() : base() { }
        public FloatReference(float constantValue) : base(constantValue) { }

        public static implicit operator float(FloatReference reference) {
            return reference.Value;
        }
    }
}