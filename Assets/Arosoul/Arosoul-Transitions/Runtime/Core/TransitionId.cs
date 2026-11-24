using System;
using UnityEngine;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Type-safe reference to a transition definition.
    /// Use the generated constants (TransitionIds.Fade) in code, or a field of this type in the inspector.
    /// The value is a stable integer stored in the definition asset, renaming or reordering definitions never breaks references.
    /// </summary>
    [Serializable]
    public struct TransitionId : IEquatable<TransitionId>
    {
        [SerializeField] private int _value;

        public TransitionId(int value)
        {
            _value = value;
        }

        public int Value => _value;
        public bool IsNone => _value == 0;
        public static TransitionId None => default;

        public bool Equals(TransitionId other) => _value == other._value;
        public override bool Equals(object obj) => obj is TransitionId other && Equals(other);
        public override int GetHashCode() => _value;

        public static bool operator ==(TransitionId a, TransitionId b) => a._value == b._value;
        public static bool operator !=(TransitionId a, TransitionId b) => a._value != b._value;

        public override string ToString()
        {
            if (IsNone) return "None";
            return TransitionRegistry.TryGetKey(this, out string key) ? key : $"TransitionId({_value})";
        }
    }
}
