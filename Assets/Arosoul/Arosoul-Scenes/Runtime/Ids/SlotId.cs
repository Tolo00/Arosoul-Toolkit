using System;
using UnityEngine;

namespace Arosoul.Scenes
{
    /// <summary>
    /// Type-safe reference to a scene slot. A slot holds at most one scene.
    /// Define slots in the Scene Config window, use the generated constants (SlotIds.Main) in code.
    /// </summary>
    [Serializable]
    public struct SlotId : IEquatable<SlotId>
    {
        [SerializeField] private int _value;

        public SlotId(int value)
        {
            _value = value;
        }

        public int Value => _value;
        public bool IsNone => _value == 0;
        public static SlotId None => default;

        public bool Equals(SlotId other) => _value == other._value;
        public override bool Equals(object obj) => obj is SlotId other && Equals(other);
        public override int GetHashCode() => _value;

        public static bool operator ==(SlotId a, SlotId b) => a._value == b._value;
        public static bool operator !=(SlotId a, SlotId b) => a._value != b._value;

        public override string ToString()
        {
            if (IsNone) return "None";
            var catalog = SceneCatalog.Instance;
            return catalog != null && catalog.TryGetSlotKey(this, out string key) ? key : $"SlotId({_value})";
        }
    }
}
