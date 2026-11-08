using System.Collections;
using UnityEngine;

namespace Arosoul.SceneSystem
{
    public abstract class Transition : MonoBehaviour
    {
        public abstract string Key { get; }

        internal abstract IEnumerator In(float inTime);
        internal abstract IEnumerator Wait(float minWaitTime);
        internal abstract IEnumerator Out(float outTime);

        internal virtual void OnCancel() { }
    }
}