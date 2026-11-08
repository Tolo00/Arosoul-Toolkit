using UnityEngine;
using System;

namespace Arosoul.Essentials {

    /// <summary>
    /// Member variable should be of type: Object
    /// Cast it to the desired Interface when using it in code.
    /// </summary>
    public class ForceInterfaceAttribute : PropertyAttribute
    {
        public readonly Type interfaceType;
        public ForceInterfaceAttribute(Type interfaceType) {
            this.interfaceType = interfaceType;
        }
    }

}
