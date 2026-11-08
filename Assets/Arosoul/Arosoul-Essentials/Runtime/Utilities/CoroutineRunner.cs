using UnityEngine;

namespace Arosoul.Essentials {

    /// <summary>
    /// Used to invoke Coroutines in scripts that doesn't inherit from 'MonoBehaviour'.
    /// Example: CoroutineRunner.Inst.StartCoroutine(MyCoroutine());
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner inst;
        public static CoroutineRunner Inst {
            get {
                if (inst == null)
                {
                    inst = FindAnyObjectByType<CoroutineRunner>(FindObjectsInactive.Include);

                    if (inst == null) {
                        GameObject go = new GameObject("CoroutineRunner");
                        inst = go.AddComponent<CoroutineRunner>();
                        DontDestroyOnLoad(inst.gameObject);
                    }
                }

                return inst;
            }
        }
    }

}
