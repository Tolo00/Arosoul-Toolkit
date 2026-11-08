using UnityEngine;
using UnityEditor;
using System;

namespace Arosoul.Essentials.Editor {

    [CustomPropertyDrawer(typeof(Array2DContainer<>))]
    public class Array2DContainerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty heightProp = property.FindPropertyRelative("height");
            int height = heightProp.intValue;

            // Return the height of the entire grid, including the label
            return EditorGUIUtility.singleLineHeight * (height + 2); // +2 for label and spacing
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Retrieve width, height, and arrayData
            SerializedProperty widthProp = property.FindPropertyRelative("width");
            SerializedProperty heightProp = property.FindPropertyRelative("height");
            SerializedProperty arrayDataProp = property.FindPropertyRelative("arrayData");

            int width = widthProp.intValue;
            int height = heightProp.intValue;

            // Draw the label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Calculate grid cell width based on available space
            float cellWidth = position.width / width;
            float cellHeight = EditorGUIUtility.singleLineHeight;

            // Get the generic type T of Array2DContainer<T> using reflection
            Type elementType = fieldInfo.FieldType.GetGenericArguments()[0];

            // Draw the grid
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    SerializedProperty cellProp = arrayDataProp.GetArrayElementAtIndex(index);

                    Rect cellRect = new Rect(
                        position.x + x * cellWidth,
                        position.y + EditorGUIUtility.singleLineHeight * (y + 1),
                        cellWidth - 5,
                        cellHeight
                    );

                    // Handle specific types based on the generic type T
                    if (cellProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (elementType.IsSubclassOf(typeof(UnityEngine.Object)) || elementType == typeof(UnityEngine.Object))
                        {
                            // Object reference type (e.g., GameObject, ScriptableObject)
                            cellProp.objectReferenceValue = EditorGUI.ObjectField(
                                cellRect, 
                                cellProp.objectReferenceValue, 
                                elementType, // Restrict to the type of T
                                true // Allow scene objects
                            );
                        }
                        else
                        {
                            // If T is not a UnityEngine.Object type, we use a regular PropertyField
                            EditorGUI.PropertyField(cellRect, cellProp, GUIContent.none);
                        }
                    }
                    else if (cellProp.propertyType == SerializedPropertyType.Vector2 && elementType == typeof(Vector2))
                    {
                        cellProp.vector2Value = EditorGUI.Vector2Field(cellRect, GUIContent.none, cellProp.vector2Value);
                    }
                    else if (cellProp.propertyType == SerializedPropertyType.Vector3 && elementType == typeof(Vector3))
                    {
                        cellProp.vector3Value = EditorGUI.Vector3Field(cellRect, GUIContent.none, cellProp.vector3Value);
                    }
                    else
                    {
                        // For other types, use PropertyField
                        EditorGUI.PropertyField(cellRect, cellProp, GUIContent.none);
                    }
                }
            }

            EditorGUI.EndProperty();
        }
    }

}