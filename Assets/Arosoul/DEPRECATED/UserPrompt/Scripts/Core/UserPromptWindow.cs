using UnityEngine;

namespace Arosoul.Tools {

    public abstract class UserPromptWindow<T> : MonoBehaviour where T : UserPromptWindow<T> {

        internal Transform canvas;

        /// <summary>
        /// Setup properties on prompt instance.
        /// </summary>
        internal abstract void Setup();
        
        internal void CleanUp() {
            Destroy(gameObject);
            if (canvas != null)
                Destroy(canvas.gameObject);
        }
    }

}