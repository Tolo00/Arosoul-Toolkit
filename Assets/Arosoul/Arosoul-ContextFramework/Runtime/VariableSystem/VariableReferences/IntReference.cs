using System;

namespace Arosoul.ContextFramework 
{
    [Serializable]
    public class IntReference : VariableReference<int, IntVariable> {
        public IntReference() : base() { }
        public IntReference(int constantValue) : base(constantValue) { }

        public static implicit operator int(IntReference reference) {
            return reference.Value;
        }
    }
}