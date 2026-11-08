using UnityEngine;
using TMPro;

namespace Arosoul.Essentials {

    [RequireComponent(typeof(TMP_Text)), ExecuteAlways]
    public class SetVersionNumber : MonoBehaviour
    {
        [SerializeField] string _prefix;
        [SerializeField] string _suffix;

        void OnEnable() {
            GetComponent<TMP_Text>().text = _prefix + Application.version + _suffix;
        }
    }

}
