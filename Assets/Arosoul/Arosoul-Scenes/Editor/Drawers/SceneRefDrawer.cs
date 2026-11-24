using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Scene picker for <see cref="SceneRef"/> fields. Stores the scene guid, with the path and name cached for display.
    /// Shows a Register button if the scene is not registered in Scene Config yet.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneRef))]
    public class SceneRefDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedObject serializedObject = property.serializedObject;
            string basePath = property.propertyPath;

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var field = new ObjectField(property.displayName)
            {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false
            };
            field.AddToClassList(BaseField<UnityEngine.Object>.alignedFieldUssClassName);
            field.style.flexGrow = 1;
            root.Add(field);

            var register = new Button { text = "Register", tooltip = "This scene is not registered in Scene Config. Registered scenes can be loaded and have dependencies." };
            register.style.display = DisplayStyle.None;
            root.Add(register);

            string CurrentGuid()
            {
                SerializedProperty guidProperty = serializedObject.FindProperty(basePath + "._guid");
                return guidProperty != null ? guidProperty.stringValue : "";
            }

            void Refresh()
            {
                string guid = CurrentGuid();
                SceneAsset asset = null;
                if (!string.IsNullOrEmpty(guid))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path)) asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                }

                field.SetValueWithoutNotify(asset);
                bool unregistered = asset != null && !ScenesSettings.instance.Data.IsRegistered(guid);
                register.style.display = unregistered ? DisplayStyle.Flex : DisplayStyle.None;
            }

            field.RegisterValueChangedCallback(evt =>
            {
                Write(serializedObject, basePath, evt.newValue as SceneAsset);
                Refresh();
            });

            register.clicked += () =>
            {
                ScenesSettingsUtility.RegisterScene(CurrentGuid());
                Refresh();
            };

            Refresh();
            root.TrackPropertyValue(property, _ => Refresh());
            return root;
        }

        private static void Write(SerializedObject serializedObject, string basePath, SceneAsset asset)
        {
            string guid = "", path = "", name = "";
            if (asset != null)
            {
                path = AssetDatabase.GetAssetPath(asset);
                guid = AssetDatabase.AssetPathToGUID(path);
                name = asset.name;
            }

            serializedObject.FindProperty(basePath + "._guid").stringValue = guid;
            serializedObject.FindProperty(basePath + "._path").stringValue = path;
            serializedObject.FindProperty(basePath + "._name").stringValue = name;
            ScenesUndo.Apply(serializedObject, "Assign Scene");
        }
    }
}
