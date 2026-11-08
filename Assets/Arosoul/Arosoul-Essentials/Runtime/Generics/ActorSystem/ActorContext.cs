using System;
using UnityEngine;

namespace Arosoul.Essentials.ActorSystem 
{
    /// <summary>
    /// Base context class for actor system. Inherit and add your serialized fields variable/references.
    /// </summary>
    [Serializable]
    public abstract class ActorContext<TActor> where TActor : MonoBehaviour
    {
        [NonSerialized] public TActor EntityComponent;

        public bool InputEnabled { get; private set; } = true;
        public void SetInputEnabled(bool enabled)
        {
            InputEnabled = enabled;
        }
    }
}