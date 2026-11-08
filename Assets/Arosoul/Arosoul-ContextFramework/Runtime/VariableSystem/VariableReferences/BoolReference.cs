using System;

namespace Arosoul.ContextFramework 
{
    [Serializable]
    public class BoolReference : VariableReference<bool, BoolVariable> {
        public BoolReference() : base() { }
        public BoolReference(bool constantValue) : base(constantValue) { }

        public static implicit operator bool(BoolReference reference) {
            return reference.Value;
        }
    }
}