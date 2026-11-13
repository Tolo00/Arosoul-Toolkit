using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

public class MainToolbarTimescaleSlider
{
    const float k_minTimeScale = 0f, k_maxTimeScale = 5f;

    [MainToolbarElement("Timescale/Slider", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement TimeSlider()
    {
        MainToolbarElementStyler.StyleElement<VisualElement>("Timescale/Slider", (element) =>
        {
            element.style.paddingLeft = 10f;
        });

        return new MainToolbarSlider(
            new MainToolbarContent("Time Scale", "Time Scale"),//Content(Text, Tooltip)
            Time.timeScale,//Value
            k_minTimeScale,//Min Value
            k_maxTimeScale,//Max Value
            value => Time.timeScale = value//Action
        );
    }

    [MainToolbarElement("Timescale/Reset", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement ResetTimeScaleButton()
    {
        MainToolbarElementStyler.StyleElement<UnityEditor.Toolbars.EditorToolbarButton>("Timescale/Reset", element =>
        {
            element.style.paddingLeft = 0f;
            element.style.paddingRight = 0f;
            element.style.marginLeft = 0f;
            element.style.marginRight = 0f;
            element.style.minWidth = 20f;
            element.style.maxWidth = 20f;

            var image = element.Q<Image>();
            if (image == null) return;
            image.style.width = 12f;
            image.style.height = 12f;
        });

        return new MainToolbarButton(
            new MainToolbarContent(EditorGUIUtility.IconContent("Refresh").image as Texture2D, "Reset"),//Content(Image, Tooltip)
            () => { Time.timeScale = 1f; MainToolbar.Refresh("Timescale/Slider"); }//Action
        );
    }
}