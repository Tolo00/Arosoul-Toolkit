using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Keep the objects transform independent from its parents transform.
    /// </summary>
    public class WorldTransform : MonoBehaviour
    {
        [Header("Toggle World Transform")]
        public bool UseWorldPosition;
        public bool UseWorldRotation;
        public bool UseWorldScale;

        Vector3 worldPos;
        Quaternion worldRot;
        Vector3 worldScale;

        Transform parent;

        void Start() {
            parent = transform.parent;

            worldPos = transform.position;
            worldRot = transform.rotation;
            worldScale = transform.lossyScale;
        }

        void LateUpdate() {
            UpdateTransform();
        }

        private void UpdateTransform() {
            if (UseWorldPosition) transform.position = worldPos;
            if (UseWorldRotation) transform.rotation = worldRot;
            if (UseWorldScale) {
                transform.SetParent(null);
                transform.localScale = worldScale;
                transform.SetParent(parent);
            }
        }



        public void SetWorldPosition(Vector3 position) {
            worldPos = position;
            UpdateTransform();
        }

        public void SetWorldRotation(Quaternion rotation) {
            worldRot = rotation;
            UpdateTransform();
        }

        public void SetWorldScale(Vector3 scale) {
            worldScale = scale;
            UpdateTransform();
        }

        public void SetParent(Transform parent) {
            this.parent = parent;
            transform.SetParent(parent);
        }
    }

}