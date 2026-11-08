using UnityEngine;
using System;

namespace Arosoul.Essentials {

    [Serializable]
    public class EnumArrayContainer<TValue, TEnum> where TEnum : Enum
    {
        [SerializeField] private TValue[] content = null;
        [SerializeField] private TEnum enumType;

        public TValue this[int i] {
            get { return content[i]; }
        }

        public TValue this[TEnum e] {
            get { return content[Convert.ToInt32(e)]; }
        }

        public int Length {
            get { return content.Length; }
        }
    }

}
