using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arosoul.Tools 
{
    public class UIBlocker : MonoBehaviour {
        private static UIBlocker inst;
        public static UIBlocker Inst {
            get {
                if (inst == null)
                {
                    inst = FindAnyObjectByType<UIBlocker>(FindObjectsInactive.Include);

                    if (inst == null) {
                        GameObject go = new GameObject("UIBlocker");
                        inst = go.AddComponent<UIBlocker>();
                        inst.SetupBlocker();
                    }
                }

                return inst;
            }
        }

        private void OnDestroy() {
            if (inst == this) {
                inst = null;
            }
        }

        Canvas canvas;
        Image imageComponent;

        private void SetupBlocker() {
            CanvasScaler canvasScaler;

            canvas = gameObject.AddComponent<Canvas>();            
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // High sorting order so it appears on top
            
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject imageGO = new GameObject("BlockerImage");
            imageGO.transform.SetParent(transform, false);
            imageGO.AddComponent<RectTransform>().ResizeToParent();

            imageComponent = imageGO.AddComponent<Image>();
            imageComponent.color = new Color(0, 0, 0, 0.5f); // semi-transparent black
        }


        public static void Show(float alpha = 0.5f, Color? setColor = null, int sortingOrder = 1000) {
            Color color = setColor == null ? Color.black : (Color)setColor;
            color = new Color(color.r, color.g, color.b, alpha);
            
            Inst.imageComponent.color = color;
            Inst.canvas.sortingOrder = sortingOrder;
            Inst.gameObject.SetActive(true);
        }

        public static void Hide(GameObject newSelectedGameObject = null) {
            Inst.gameObject.SetActive(false);
            if (newSelectedGameObject != null) 
                EventSystem.current.SetSelectedGameObject(newSelectedGameObject);
        }
    }

}