using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Reset assigned gameObject transform properties every frame. 
    /// </summary>
    public class ResetTransform : MonoBehaviour
    {
        [SerializeField] bool position = true;
        [SerializeField] bool rotation = true;
        [SerializeField] bool scale = true;

        void Update()
        {
            if (position) transform.localPosition = Vector3.zero;
            if (rotation) transform.localRotation = Quaternion.Euler(Vector3.zero);
            if (scale) transform.localScale = Vector3.zero;
        }
    }

}
