using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class SceneSelectorToolbar
{
    internal static Texture2D SceneIcon =>
        EditorGUIUtility.ObjectContent(null, typeof(SceneAsset)).image as Texture2D;

    [MainToolbarElement("Editor Utility/Scene Selector", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement Create()
    {
        var content = new MainToolbarContent(SceneIcon, "Scene Selector");
        return new MainToolbarDropdown(content, rect => UnityEditor.PopupWindow.Show(rect, new SceneSelectorPopup()));
    }
}

public class SceneSelectorPopup : PopupWindowContent
{
    private static string FavKey => $"SceneSelector.Favorites.{PlayerSettings.productGUID}";

    private static readonly Color AdditiveColor = new(0.25f, 0.55f, 0.95f, 1f);
    private static readonly Color StarColor = new(1f, 0.8f, 0.2f, 1f);

    private struct SceneEntry { public string Path, Guid, Name; }

    private List<SceneEntry> _scenes;
    private HashSet<string> _favorites;
    private VisualElement _root, _list;
    private Label _header;
    private readonly List<VisualElement> _additiveBadges = new();
    private bool _shift;

    private EditorApplication.CallbackFunction _modifierHandler;
    
    public SceneSelectorPopup() => LoadData();

    public override Vector2 GetWindowSize()
    {
        float h = 28 + _scenes.Count * 24 + (_favorites.Count > 0 ? 8 : 0);
        return new Vector2(280, Mathf.Clamp(h, 60, 400));
    }

    private void LoadData()
    {
        _scenes = AssetDatabase.FindAssets("t:Scene")
            .Select(g => { var p = AssetDatabase.GUIDToAssetPath(g); return new SceneEntry { Guid = g, Path = p, Name = System.IO.Path.GetFileNameWithoutExtension(p) }; })
            .Where(s => IsEditable(s.Path)) // Filter out scenes from read-only packages
            .OrderBy(s => s.Name).ToList();

        _favorites = new HashSet<string>(EditorPrefs.GetString(FavKey, "").Split(';', System.StringSplitOptions.RemoveEmptyEntries));
    }

    private void SaveFavorites() => EditorPrefs.SetString(FavKey, string.Join(";", _favorites));

    public override void OnGUI(Rect rect) { }

    public override void OnOpen()
    {
        _root = editorWindow.rootVisualElement;
        _root.focusable = true;
        _root.RegisterCallback<KeyDownEvent>(e => { if (IsShift(e.keyCode)) SetShift(true); }, TrickleDown.TrickleDown);
        _root.RegisterCallback<KeyUpEvent>(e => { if (IsShift(e.keyCode)) SetShift(false); }, TrickleDown.TrickleDown);
        _root.RegisterCallback<PointerMoveEvent>(e => SetShift(e.shiftKey), TrickleDown.TrickleDown);

        _header = new Label();
        _header.style.height = 22;
        _header.style.unityTextAlign = TextAnchor.MiddleCenter;
        _header.style.unityFontStyleAndWeight = FontStyle.Bold;
        _header.style.fontSize = 11;
        _root.Add(_header);

        var scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        _list = scroll;
        _root.Add(scroll);

        var poller = new IMGUIContainer(() =>
        {
            if (Event.current.type == EventType.Repaint && Event.current.shift != _shift)
                SetShift(Event.current.shift);
        });
        poller.style.position = Position.Absolute;
        poller.style.width = poller.style.height = 1;
        poller.pickingMode = PickingMode.Ignore;
        _root.Add(poller);

        _modifierHandler = () => poller.MarkDirtyRepaint();
        EditorApplication.modifierKeysChanged += _modifierHandler;
        poller.schedule.Execute(() => poller.MarkDirtyRepaint()).Every(30);

        Rebuild();
        SetShift(Event.current != null && Event.current.shift);
        _root.schedule.Execute(() => _root.Focus());
    }

    public override void OnClose()
    {
        if (_modifierHandler != null) EditorApplication.modifierKeysChanged -= _modifierHandler;
    }

    private static bool IsShift(KeyCode k) => k == KeyCode.LeftShift || k == KeyCode.RightShift;

    private void SetShift(bool shift)
    {
        _shift = shift;
        _header.text = shift ? "+ Additive load" : "Single load (hold Shift for additive)";
        _header.style.backgroundColor = shift ? AdditiveColor : new Color(0, 0, 0, 0.15f);
        _header.style.color = shift ? Color.white : new StyleColor(StyleKeyword.Null);
        foreach (var b in _additiveBadges) b.style.display = shift ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Rebuild()
    {
        _list.Clear();
        _additiveBadges.Clear();

        var favs = _scenes.Where(s => _favorites.Contains(s.Guid)).ToList();
        var rest = _scenes.Where(s => !_favorites.Contains(s.Guid)).ToList();

        foreach (var s in favs) _list.Add(CreateRow(s, true));
        if (favs.Count > 0 && rest.Count > 0)
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.marginTop = sep.style.marginBottom = 3;
            sep.style.backgroundColor = new Color(1, 1, 1, 0.2f);
            _list.Add(sep);
        }
        foreach (var s in rest) _list.Add(CreateRow(s, false));

        SetShift(_shift);
    }

    private VisualElement CreateRow(SceneEntry scene, bool favorite)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.height = 24;
        row.style.paddingLeft = 6;
        row.style.paddingRight = 6;

        row.RegisterCallback<PointerEnterEvent>(_ => row.style.backgroundColor = new Color(1, 1, 1, 0.1f));
        row.RegisterCallback<PointerLeaveEvent>(_ => row.style.backgroundColor = StyleKeyword.Null);

        var plus = new Label("+");
        plus.style.color = AdditiveColor;
        plus.style.unityFontStyleAndWeight = FontStyle.Bold;
        plus.style.width = 12;
        plus.style.display = DisplayStyle.None;
        _additiveBadges.Add(plus);
        row.Add(plus);

        var icon = new Image { image = SceneSelectorToolbar.SceneIcon };
        icon.style.width = icon.style.height = 16;
        icon.style.marginRight = 4;
        row.Add(icon);

        var label = new Label(scene.Name);
        label.style.flexGrow = 1;
        label.style.overflow = Overflow.Hidden;
        label.style.textOverflow = TextOverflow.Ellipsis;
        if (IsSceneOpen(scene.Path)) label.style.unityFontStyleAndWeight = FontStyle.Bold;
        row.Add(label);

        var star = new StarElement(favorite, StarColor);
        star.RegisterCallback<ClickEvent>(e =>
        {
            if (!_favorites.Add(scene.Guid)) _favorites.Remove(scene.Guid);
            SaveFavorites();
            Rebuild();
            e.StopPropagation();
        });
        row.Add(star);

        row.RegisterCallback<ClickEvent>(e => LoadScene(scene.Path, e.shiftKey));
        return row;
    }

    private static bool IsSceneOpen(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).path == path) return true;
        return false;
    }

    private static bool IsEditable(string path)
    {
        if (!path.StartsWith("Packages/")) return true;
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
        return info == null
            || info.source == UnityEditor.PackageManager.PackageSource.Embedded
            || info.source == UnityEditor.PackageManager.PackageSource.Local;
    }

    private void LoadScene(string path, bool additive)
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Scene Selector: can't open scenes in the editor while in Play Mode.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
        if (additive) Rebuild();
        else editorWindow.Close();
    }

    private class StarElement : VisualElement
    {
        private readonly bool _filled;
        private readonly Color _color;

        public StarElement(bool filled, Color color)
        {
            _filled = filled;
            _color = color;
            style.width = style.height = 18;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            var c = contentRect.center;
            float outer = Mathf.Min(contentRect.width, contentRect.height) * 0.5f - 1f;
            float inner = outer * 0.42f;

            p.BeginPath();
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? outer : inner;
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                var pt = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.ClosePath();

            if (_filled) { p.fillColor = _color; p.Fill(); }
            else { p.strokeColor = _color; p.lineWidth = 1.2f; p.lineJoin = LineJoin.Round; p.Stroke(); }
        }
    }
}