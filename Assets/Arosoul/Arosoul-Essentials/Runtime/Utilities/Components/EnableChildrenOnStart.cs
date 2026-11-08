using UnityEngine;

namespace Arosoul.Essentials {

    public class EnableChildrenOnStart : MonoBehaviour
    {
        private void Start()
        {
            foreach (Transform child in transform) {
                child.gameObject.SetActive(true);
            }
        }
    }

}
