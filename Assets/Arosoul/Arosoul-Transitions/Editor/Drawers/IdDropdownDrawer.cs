using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Base for property drawers of id structs that wrap a single int field named _value.
    /// Shows a dropdown with all available options.
    /// </summary>
    public abstract class IdDropdownDrawer : PropertyDrawer
    {
        protected const string ValueFieldName = "_value";

        /// <summary>
        /// All selectable options. The None option (id 0) is added automatically.
        /// </summary>
        protected abstract List<(int id, string label)> GetOptions();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var serializedObject = property.serializedObject;
            string valuePath = property.FindPropertyRelative(ValueFieldName).propertyPath;
            string displayName = property.displayName;

            var options = new List<(int id, string label)> { (0, "None") };
            options.AddRange(GetOptions());

            int current = property.FindPropertyRelative(ValueFieldName).intValue;
            int index = options.FindIndex(o => o.id == current);
            if (index < 0)
            {
                options.Add((current, $"Missing ({current})"));
                index = options.Count - 1;
            }

            var labels = options.ConvertAll(o => o.label);
            var dropdown = new DropdownField(property.displayName, labels, index);
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            dropdown.RegisterValueChangedCallback(_ =>
            {
                int selected = dropdown.index;
                if (selected < 0 || selected >= options.Count) return;

                var valueProperty = serializedObject.FindProperty(valuePath);
                if (valueProperty == null || valueProperty.intValue == options[selected].id) return;

                Undo.IncrementCurrentGroup();
                valueProperty.intValue = options[selected].id;
                if (serializedObject.ApplyModifiedProperties())
                    Undo.SetCurrentGroupName($"Change {displayName}");
            });

            return dropdown;
        }
    }
}
