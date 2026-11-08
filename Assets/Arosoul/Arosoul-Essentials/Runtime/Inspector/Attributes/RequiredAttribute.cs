using System;
using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Display a required error message if field is null.
    /// Prevents the game from starting while requirement is not meet.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequiredAttribute : PropertyAttribute { 
        public WarningType warningType;

        public RequiredAttribute(WarningType warningType = WarningType.Error) {
            this.warningType = warningType;
        }
    }

    public enum WarningType { InspectorWarning, Error }

}
