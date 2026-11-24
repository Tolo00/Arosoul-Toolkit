using System.Collections.Generic;
using Arosoul.Scenes.Editor;
using NUnit.Framework;

namespace Arosoul.Scenes.Tests
{
    public class ScenesValidatorTests
    {
        private sealed class FakeContext : ISceneProjectContext
        {
            public readonly HashSet<string> Existing = new HashSet<string>();
            public readonly Dictionary<string, BuildState> Build = new Dictionary<string, BuildState>();

            public bool SceneExists(string guid) => Existing.Contains(guid);
            public BuildState GetBuildState(string guid) => Build.TryGetValue(guid, out var state) ? state : BuildState.NotInBuild;
        }

        private static SceneRef Scene(string guid) => new SceneRef(guid, $"Assets/{guid}.unity", guid);

        private FakeContext _context;
        private ScenesData _data;

        [SetUp]
        public void SetUp()
        {
            _context = new FakeContext();
            _data = new ScenesData();
            SlotDefinition main = _data.AddSlot("Main");
            _data.SetInitialSlot(main.Id);
        }

        private void Register(string guid, params string[] dependencies)
        {
            _context.Existing.Add(guid);
            _context.Build[guid] = BuildState.Enabled;
            var refs = new SceneRef[dependencies.Length];
            for (int i = 0; i < refs.Length; i++) refs[i] = Scene(dependencies[i]);
            _data.AddScene(Scene(guid), refs);
        }

        private ValidationReport Validate(bool synced = false) => ScenesValidator.Validate(_data, _context, synced);

        [Test]
        public void ValidSetup_HasNoIssues()
        {
            Register("Managers");
            Register("Gameplay", "Managers");

            var report = Validate();
            Assert.AreEqual(0, report.ErrorCount, string.Join("\n", report.Issues));
            Assert.AreEqual(0, report.WarningCount, string.Join("\n", report.Issues));
        }

        [Test]
        public void MissingInitialSlot_IsError()
        {
            _data = new ScenesData();
            _data.AddSlot("Main");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "initial slot"));
        }

        [Test]
        public void NoSlots_IsWarning()
        {
            _data = new ScenesData();
            var report = Validate();
            Assert.IsTrue(report.Contains(ValidationSeverity.Warning, "No slots"));
        }

        [Test]
        public void DuplicateSlotKeys_AreErrors()
        {
            _data.AddSlot("Main");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "collides"));
        }

        [Test]
        public void SlotKeysThatSanitizeToSameName_AreErrors()
        {
            _data.AddSlot("Ui Layer");
            _data.AddSlot("UiLayer");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "UiLayer"));
        }

        [Test]
        public void DeletedScene_IsError()
        {
            Register("Gameplay");
            _context.Existing.Remove("Gameplay");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "no longer exists"));
        }

        [Test]
        public void SceneRegisteredTwice_IsError()
        {
            Register("Gameplay");
            Register("Gameplay");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "more than once"));
        }

        [Test]
        public void SelfDependency_IsError()
        {
            Register("Gameplay", "Gameplay");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "depends on itself"));
        }

        [Test]
        public void DependencyCycle_IsError()
        {
            Register("A", "B");
            Register("B", "A");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "cycle"));
        }

        [Test]
        public void UnregisteredDependency_IsWarning()
        {
            Register("Gameplay", "Managers");
            _context.Existing.Add("Managers");
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Warning, "not registered"));
        }

        [Test]
        public void SceneNotInBuild_IsWarning_UnlessBuildSettingsAreSynced()
        {
            Register("Gameplay");
            _context.Build["Gameplay"] = BuildState.NotInBuild;

            Assert.IsTrue(Validate(synced: false).Contains(ValidationSeverity.Warning, "not in the build settings"));
            Assert.IsFalse(Validate(synced: true).Contains(ValidationSeverity.Warning, "not in the build settings"));
        }

        [Test]
        public void CoreEnabledWithoutScene_IsError()
        {
            _data.SetCore(true, default);
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "core scene is enabled"));
        }

        [Test]
        public void CoreWithDependencies_IsError()
        {
            Register("Managers");
            Register("Core", "Managers");
            _data.SetCore(true, Scene("Core"));
            Assert.IsTrue(Validate().Contains(ValidationSeverity.Error, "core scene can not have dependencies"));
        }

        [Test]
        public void DisabledCore_IsNotValidated()
        {
            _data.SetCore(false, default);
            Assert.AreEqual(0, Validate().ErrorCount);
        }

    }

    public class ScenesDataTests
    {
        [Test]
        public void Clone_IsDeepCopy()
        {
            var data = new ScenesData();
            data.AddSlot("Main");
            data.AddScene(new SceneRef("a", "Assets/a.unity", "a"), new SceneRef("b"));

            ScenesData clone = data.Clone();
            Assert.IsTrue(data.IsSameAs(clone));
            Assert.AreNotSame(data.Scenes[0], clone.Scenes[0]);
            Assert.AreEqual("b", clone.Scenes[0].Dependencies[0].Guid);
        }

        [Test]
        public void RefreshCaches_UpdatesPathAndName_AndKeepsGuid()
        {
            var data = new ScenesData();
            data.AddScene(new SceneRef("a", "Assets/Old.unity", "Old"));

            data.RefreshCaches(guid => ("Assets/Moved/New.unity", "New"));

            SceneRef scene = data.Scenes[0].Scene;
            Assert.AreEqual("a", scene.Guid);
            Assert.AreEqual("Assets/Moved/New.unity", scene.Path);
            Assert.AreEqual("New", scene.Name);
        }

        [Test]
        public void RefreshCaches_KeepsOldCache_WhenSceneCanNotBeResolved()
        {
            var data = new ScenesData();
            data.AddScene(new SceneRef("a", "Assets/Old.unity", "Old"));

            data.RefreshCaches(guid => (null, null));

            Assert.AreEqual("Old", data.Scenes[0].Scene.Name);
        }

        [Test]
        public void SceneRef_EqualityIsByGuid()
        {
            Assert.AreEqual(new SceneRef("a", "x", "x"), new SceneRef("a", "y", "y"));
            Assert.AreNotEqual(new SceneRef("a"), new SceneRef("b"));
            Assert.IsFalse(default(SceneRef).IsValid);
        }

        [Test]
        public void SceneRef_NameFallsBackToFileName()
        {
            Assert.AreEqual("Level1", new SceneRef("a", "Assets/Scenes/Level1.unity").Name);
        }

        [Test]
        public void ActiveCoreGuid_IsNullWhenDisabledOrUnassigned()
        {
            var data = new ScenesData();
            Assert.IsNull(data.ActiveCoreGuid);

            data.SetCore(true, default);
            Assert.IsNull(data.ActiveCoreGuid);

            data.SetCore(true, new SceneRef("core"));
            Assert.AreEqual("core", data.ActiveCoreGuid);

            data.SetCore(false, new SceneRef("core"));
            Assert.IsNull(data.ActiveCoreGuid);
        }
    }
}
