using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.ContextFramework 
{
    public interface IRuntimeSet {
        void AddObject(Object obj);
        void RemoveObject(Object obj);

        IReadOnlyList<Object> ItemObjects { get; }
        System.Type SetType { get; }
    }

}