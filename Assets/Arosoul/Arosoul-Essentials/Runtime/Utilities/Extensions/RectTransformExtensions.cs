using UnityEngine;

public static class RectTransformExtensions 
{
    public static void ResizeToParent(this RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

}
