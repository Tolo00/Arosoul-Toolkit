using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Override gameObject transform with values from targetTransform.
    /// </summary>
    public class SetTransform : MonoBehaviour
    {
        [SerializeField] Transform targetTransfrom;

        [Space]

        [SerializeField] bool position = true;
        [SerializeField] bool rotation = true;
        [SerializeField] bool scale = true;

        void FixedUpdate()
        {
            if (position) transform.position = targetTransfrom.position;
            if (rotation) transform.rotation = targetTransfrom.rotation;
            if (scale) transform.localScale = targetTransfrom.localScale;
        }
    }

}