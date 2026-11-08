using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arosoul.Essentials.Editor {

    [InitializeOnLoad]
    public static class RequiredFieldValidator {
        static RequiredFieldValidator() {
            EditorApplication.playModeStateChanged += (state) => {
                if (state == PlayModeStateChange.ExitingEditMode) {
                    CheckFields();
                }
            };
        }
    
        private static void CheckFields() {
            bool shouldPauseGame = false;
            MonoBehaviour[] monoBehaviours = GameObject.FindObjectsByType<MonoBehaviour>();

            foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
                FieldInfo[] fields = monoBehaviour.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields) {
                    RequiredAttribute requiredAttribute = field.GetCustomAttribute<RequiredAttribute>();

                    if (requiredAttribute != null) {
                        object fieldValue = field.GetValue(monoBehaviour);

                        if (fieldValue == null && requiredAttribute.warningType == WarningType.Error) {
                            Debug.LogError($"The field {field.Name} is required.", monoBehaviour);
                            shouldPauseGame = true;
                        }
                    }
                }
            }

            if (shouldPauseGame) {
                EditorApplication.ExitPlaymode();
            }
        }
    }

}
