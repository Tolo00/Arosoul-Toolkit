using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Arosoul.SceneSystem
{
    public class FadeTransition : Transition {
        [SerializeField] Image _image;

        public override string Key => "Fade";

        void Awake() {
            gameObject.SetActive(true);
            SetImageAlpha(0);
        }

        internal override IEnumerator In(float inTime) {
            float timer = 0;
            float alpha = 0;
            _image.enabled = true;
            SetImageAlpha(0);

            while (timer < inTime) { 
                timer += Time.deltaTime;
                alpha = timer/inTime;
                
                alpha = Mathf.Min(alpha, 1);
                SetImageAlpha(alpha);

                yield return null;
            }

            SetImageAlpha(1);
        }

        internal override IEnumerator Wait(float minWaitTime) {
            float timer = 0;
            
            while (timer < minWaitTime) {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        internal override IEnumerator Out(float outTime) {
            float timer = 0;
            float alpha = 1;
            SetImageAlpha(alpha);

            while (timer < outTime) { 
                timer += Time.deltaTime;
                alpha = 1 - timer/outTime;
                
                alpha = Mathf.Max(alpha, 0);
                SetImageAlpha(alpha);

                yield return null;
            }

            SetImageAlpha(0);
            _image.enabled = false;
        }

        private void SetImageAlpha(float alpha) {
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, alpha);
        }
    }
}