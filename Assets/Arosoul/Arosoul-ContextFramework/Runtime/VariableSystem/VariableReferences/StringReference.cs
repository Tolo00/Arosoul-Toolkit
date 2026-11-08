using System;

namespace Arosoul.ContextFramework 
{
    [Serializable]
    public class StringReference : VariableReference<string, StringVariable> {
        public StringReference() : base() { }
        public StringReference(string constantValue) : base(constantValue) { }

        public static implicit operator string(StringReference reference) {
            return reference.Value;
        }
    }
}