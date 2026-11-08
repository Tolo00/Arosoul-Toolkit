using UnityEngine;

namespace Arosoul.Essentials {

    public class DividerAttribute : PropertyAttribute {
        public readonly float thickness;
        public readonly float upperPadding;
        public readonly float bottomPadding;

        public DividerAttribute(float thickness = 1f, float upperPadding = 0.75f, float bottomPadding = 1f) {
            this.thickness = thickness;
            this.upperPadding = upperPadding;
            this.bottomPadding = bottomPadding;
        }
    }

}
