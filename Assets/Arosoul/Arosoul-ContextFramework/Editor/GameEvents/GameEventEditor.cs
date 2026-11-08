using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.UIElements;

namespace Arosoul.ContextFramework.Editor 
{
    [CustomEditor(typeof(GameEvent))]
    public class GameEventEditor : UnityEditor.Editor {
        
        public VisualTreeAsset VisualTree;

        private Button _raiseButton;
        private Button _debugModeEnabler;
        private VisualElement _debugPanel;
        private VisualElement _listenerContainer;
        private VisualElement _historyContainer;

        private GameEvent _script;

        void OnEnable() {
            _script = target as GameEvent;
            EditorApplication.update += RefreshInspectorUI;
        }

        void OnDisable() {
            EditorApplication.update -= RefreshInspectorUI;
        }

        public override VisualElement CreateInspectorGUI() {
            VisualElement root = new VisualElement();
            VisualTree.CloneTree(root);

            // Event name label
            Label eventNameLabel = (Label)root.Q("event-name-label");
            eventNameLabel.text = _script.name;

            // Raise button
            _raiseButton = (Button)root.Q("raise-button");
            _raiseButton.RegisterCallback<ClickEvent>(e => {
                _script.Raise(_script);
                Debug.Log($"Event ({_script.name}) was raised through the editor.");
            });

            // Debug mode enabler
            _debugModeEnabler = (Button)root.Q("debug-mode-enabler");
            _debugModeEnabler.RegisterCallback<ClickEvent>(e => {
                _script.SetDebugMode(!_script.DebugMode);
                _script.ClearLogs();
            });

            // DEBUG MODE
            _debugPanel = root.Q("debug-panel");
            _debugPanel.visible = _script.DebugMode;

            _listenerContainer = root.Q("listener-container");
            _historyContainer = root.Q("history-container");

            return root;
        }

        private void RefreshInspectorUI() {
            _raiseButton?.SetEnabled(EditorApplication.isPlaying);

            if (_debugModeEnabler != null) {
                _debugModeEnabler.SetEnabled(EditorApplication.isPlaying);
                _debugModeEnabler.text = _script.DebugMode ? "Disable" : "Enable";
            }

            if (_debugPanel != null)
                _debugPanel.visible = _script.DebugMode;

            if (_listenerContainer != null) RefreshListeners();
            if (_historyContainer != null) RefreshHistory();   
        }

        private void RefreshListeners() {
            _listenerContainer.Clear();
            List<ListenerInfo> listeners = GetListeners();

            for (int i = 0; i < listeners.Count; i++) {
                ListenerInfo listener = listeners[i];

                // Create an ObjectField
                var objectField = new ObjectField {
                    objectType = typeof(UnityEngine.Object),   // or typeof(SpecificType) if you know it
                    value = listener.unityObject,
                    label = ""
                };

                objectField.SetEnabled(false);

                _listenerContainer.Add(objectField);
            }
        }

        private void RefreshHistory() {
            _historyContainer.Clear();
            var historyEnties = GetHistory();

            for (int i = 0; i < historyEnties.Length; i++) {
                GameEvent.EventRaisedLog entry = historyEnties[i]; 
                string senderName = entry.Sender == _script ? "Editor" : entry.Sender.ToString();
                var label = new Label($"{entry.DateTime.ToString("[HH:mm:ss]")} {senderName}") {
                    style = {
                        backgroundColor = i % 2 == 0 ? new StyleColor(new Color(0.3f, 0.3f, 0.3f)) : new StyleColor(new Color(0.2f, 0.2f, 0.2f)),
                        paddingTop = 2f,
                        paddingBottom = 2f,
                        paddingLeft = 5f,
                        whiteSpace = WhiteSpace.Normal
                    }
                };

                label.RegisterCallback<MouseDownEvent>(evt => {
                    if (evt.button == (int)MouseButton.LeftMouse) {
                        if (entry.Sender != null) {
                            EditorGUIUtility.PingObject(entry.Sender);
                        }
                        evt.StopPropagation();
                    }
                });
                _historyContainer.Add(label);
            }
        }



        private struct ListenerInfo {
            public string displayName;
            public UnityEngine.Object unityObject; // null if not a UnityEngine.Object
        }

        private List<ListenerInfo> GetListeners() {
            var list = new List<ListenerInfo>();
            foreach (var listener in _script.GetListeners()) { // GameEventDelegate[] 
                UnityEngine.Object targetObj = listener.Target as UnityEngine.Object;

                string name;
                if (targetObj != null) {
                    name = targetObj is GameEventListener ? $"{targetObj.name} (GameEventListener)" : targetObj.ToString();
                }
                else if (listener.Target != null)
                    name = listener.Target.GetType().Name + " (C#)";
                else
                    name = listener.Method.Name + " (static)";

                list.Add(new ListenerInfo { displayName = name, unityObject = targetObj });
            }
            return list;
        }


        private GameEvent.EventRaisedLog[] GetHistory() {
            return _script.GetRaiseLogs();
        }
    }

}