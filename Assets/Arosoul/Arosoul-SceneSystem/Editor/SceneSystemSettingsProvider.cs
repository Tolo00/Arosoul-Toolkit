using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.SceneSystem.Editor
{
    static class SceneSystemSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Player/Scene System", SettingsScope.Project)
            {
                label = "Scene System",
                
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = SceneSystemSettings.GetSerializedSettings();
                    
                    var title = new Label("Scene System Settings")
                    {
                        style = { fontSize = 19, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10, paddingLeft = 10 }
                    };
                    rootElement.Add(title);
                    
                    AddInitializationSection(rootElement, settings);
                    AddTransitionSection(rootElement, settings);
                },
                
                keywords = new[] { "Scene", "Core", "Manager", "Load" }
            };
            
            return provider;
        }

        private static void AddInitializationSection(VisualElement root, SerializedObject settings)
        {
            var properties = new VisualElement()
            {
                style = { paddingLeft = 10, paddingRight = 10, paddingBottom = 25 }
            };
            
            var coreSceneHeader = new Label("Initialization")
            {
                style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, paddingLeft = 4, paddingBottom = 5 }
            };
            properties.Add(coreSceneHeader);

            // Load Core On Startup - Field
            var loadOnStartupProp = settings.FindProperty("_loadCoreSceneOnStartup");
            var loadOnStartupField = new PropertyField(loadOnStartupProp, "Load Core On Startup");
            loadOnStartupField.Bind(settings);
            properties.Add(loadOnStartupField);

            // Core Scene Name - Field
            var coreSceneField = new PropertyField(settings.FindProperty("_coreSceneName"), "|- Scene Name");
            coreSceneField.style.display = loadOnStartupProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            loadOnStartupField.RegisterValueChangeCallback(evt =>
            {
                settings.ApplyModifiedProperties();

                coreSceneField.style.display =
                    evt.changedProperty.boolValue
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            });

            coreSceneField.Bind(settings);
            properties.Add(coreSceneField);

            // Info box
            var infoBox = new HelpBox(
                "The Core Scene acts as a bootloader for your game. It loads before any other scene " +
                "and should contain all persistent managers, singletons, and dependencies that other " +
                "scenes rely on. This ensures these systems are always available regardless of which " +
                "scene you start from in the editor or at runtime.",
                HelpBoxMessageType.Info
            );
            infoBox.style.marginTop = 5;
            infoBox.style.marginBottom = 5;
            properties.Add(infoBox);

            // Initial Scene Slot - Field
            var initialSceneSlot = new PropertyField(settings.FindProperty("_initialSceneSlot"), "Initial Scene Slot");
            initialSceneSlot.Bind(settings);
            properties.Add(initialSceneSlot);
            
            root.Add(properties);
        }

        private static void AddTransitionSection(VisualElement root, SerializedObject settings)
        {
            var properties = new VisualElement()
            {
                style = { paddingLeft = 10, paddingRight = 10, paddingBottom = 25 }
            };
            
            var transitionHeader = new Label("Registry")
            {
                style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, paddingLeft = 4, paddingBottom = 5 }
            };
            properties.Add(transitionHeader);

            // Transition list
            var transitionsProperty = settings.FindProperty("_registeredTransitions");
            var transitionsField = new PropertyField(transitionsProperty, "Registered Transitions");
            transitionsField.Bind(settings);
            properties.Add(transitionsField);

            // Regenerate enum script
            // var regenerateButton = new Button(() => { TransitionEnumGenerator.Generate(); })
            // {
            //     text = "Regenerate Transition Enum",
            //     style = { marginTop = 5 }
            // };
            // properties.Add(regenerateButton);

            root.Add(properties);
        }

    }
}