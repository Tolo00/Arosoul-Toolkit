using Arosoul.GameInput.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arosoul.GameInput.Tests {

    public class GameActionsGeneratorTests {
        InputActionAsset asset;
        GameInputConfig config;

        [SetUp]
        public void SetUp() {
            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            config = ScriptableObject.CreateInstance<GameInputConfig>();

            var serialized = new SerializedObject(config);
            serialized.FindProperty("actions").objectReferenceValue = asset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(asset);
        }

        [TestCase(InputActionType.Button, null, "ButtonAction")]
        [TestCase(InputActionType.Button, "Button", "ButtonAction")]
        [TestCase(InputActionType.Value, "Vector2", "Vector2Action")]
        [TestCase(InputActionType.Value, "Axis", "AnalogAction")]
        [TestCase(InputActionType.Value, "Analog", "AnalogAction")]
        [TestCase(InputActionType.Value, "Vector3", null)]
        public void WrapperTypeFor_FollowsControlType(InputActionType type, string controlType, string expected) {
            var action = GameInputAgentTools.AddAction(asset, "Player", "Test", type, controlType);
            Assert.AreEqual(expected, GameActionsGenerator.WrapperTypeFor(action));
        }

        [Test]
        public void WrapperTypeFor_PrioritySuffix_WinsOverControlType() {
            var action = GameInputAgentTools.AddAction(asset, "UI", "BackPriority");
            Assert.AreEqual("PriorityAction", GameActionsGenerator.WrapperTypeFor(action));

            var bare = GameInputAgentTools.AddAction(asset, "UI", "Priority");
            Assert.AreEqual("ButtonAction", GameActionsGenerator.WrapperTypeFor(bare));
        }

        [Test]
        public void Build_GeneratesGlobalClass_AndSkipsExcludedMaps() {
            GameInputAgentTools.AddAction(asset, "Player", "Jump");
            GameInputAgentTools.AddAction(asset, "Player", "Move", InputActionType.Value, "Vector2");
            GameInputAgentTools.AddAction(asset, "UI", "Submit");

            string code = GameActionsGenerator.Build(config);

            StringAssert.DoesNotContain("namespace", code);
            StringAssert.Contains("using Arosoul.GameInput;", code);
            StringAssert.Contains("\npublic static class GameActions\n{\n", code);
            StringAssert.Contains("public static readonly ButtonAction Jump = new ButtonAction(\"Player\", \"Jump\");", code);
            StringAssert.Contains("public static readonly Vector2Action Move = new Vector2Action(\"Player\", \"Move\");", code);
            StringAssert.DoesNotContain("Submit", code); // UI is excluded by default
        }

        [Test]
        public void Build_SanitizesAndDeduplicatesNames() {
            GameInputAgentTools.AddAction(asset, "Player", "Enable");
            GameInputAgentTools.AddAction(asset, "Player", "2D Look");
            GameInputAgentTools.AddAction(asset, "All", "Jump");

            string code = GameActionsGenerator.Build(config);

            StringAssert.Contains("ButtonAction Enable2 = ", code);
            StringAssert.Contains("ButtonAction _2DLook = ", code);
            StringAssert.Contains("public static class All2", code);
        }

        [Test]
        public void AgentTools_EditActionsInMemory() {
            GameInputAgentTools.AddAction(asset, "Player", "Dash");
            GameInputAgentTools.AddBinding(asset, "Player/Dash", "<Keyboard>/leftShift", "Keyboard&Mouse");
            GameInputAgentTools.AddAction(asset, "Player", "Move", InputActionType.Value, "Vector2");
            GameInputAgentTools.AddVector2Composite(asset, "Player/Move", "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d");

            var dash = asset.FindAction("Player/Dash");
            Assert.AreEqual("<Keyboard>/leftShift", dash.bindings[0].path);
            Assert.AreEqual("Keyboard&Mouse", dash.bindings[0].groups);
            Assert.AreEqual(5, asset.FindAction("Player/Move").bindings.Count); // composite + 4 parts

            Assert.Throws<System.InvalidOperationException>(() => GameInputAgentTools.AddAction(asset, "Player", "Dash"));
            Assert.Throws<System.ArgumentException>(() => GameInputAgentTools.AddBinding(asset, "Player/Missing", "<Keyboard>/a"));

            Assert.IsTrue(GameInputAgentTools.RemoveAction(asset, "Player/Dash"));
            Assert.IsFalse(GameInputAgentTools.RemoveAction(asset, "Player/Dash"));
        }
    }

}
