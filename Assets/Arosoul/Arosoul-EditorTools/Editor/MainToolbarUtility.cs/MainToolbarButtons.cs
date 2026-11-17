using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

public static class MainToolbarButtons
{
    [MainToolbarElement("Editor Utility/Open Project Settings", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarButton ProjectSettingsButton()
    {
        var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
        var content = new MainToolbarContent(icon);
        return new MainToolbarButton(content, () => { SettingsService.OpenProjectSettings(); });
    }
}
