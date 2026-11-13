using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Arosoul.Essentials {

    [Serializable]
    public class EnumArrayContainer<TValue, TEnum>  : IEnumerable<TValue> where TEnum : Enum
    {
        [SerializeField] private TValue[] content = null;
        [SerializeField] private TEnum enumType;

        public TValue this[int i] {
            get { return content[i]; }
        }

        public TValue this[TEnum e] {
            get { return content[Convert.ToInt32(e)]; }
        }

        public TValue GetValue(int i)
        {
            return content[i];
        }

        public TEnum GetEnum(int i)
        {
            return GetEnumFromValue(content[i]);
        }

        public TEnum GetEnumFromValue(TValue value)
        {
            int i = Array.IndexOf(content, value);
            if (i < 0) throw new ArgumentException($"Value '{value}' not found.");
            return (TEnum)Enum.ToObject(typeof(TEnum), i);
        }

        public int Length {
            get { return content.Length; }
        }

        // Foreach support
        public IEnumerator<TValue> GetEnumerator()
        {
            return ((IEnumerable<TValue>)content).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return content.GetEnumerator();
        }
    }

}
