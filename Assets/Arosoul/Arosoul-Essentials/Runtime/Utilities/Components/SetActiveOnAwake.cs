using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Set gameObject state when initialized.
    /// Useful to limit visible clutter when working in editor.
    /// </summary>
    public class SetActiveOnAwake : MonoBehaviour
    {
        [SerializeField] bool active;

        private void Awake()
        {
            gameObject.SetActive(active);
        }
    }

}
