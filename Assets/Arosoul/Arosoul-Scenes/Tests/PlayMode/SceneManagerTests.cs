using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Arosoul.Transitions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arosoul.Scenes.Tests
{
    public class SceneManagerTests
    {
        private SceneManager _manager;
        private FakeSceneLoader _loader;
        private SceneCatalog _catalog;
        private SlotId _main;
        private SlotId _ui;

        private static string Path(string name) => $"Assets/{name}.unity";

        [SetUp]
        public void SetUp()
        {
            var data = new ScenesData();
            _main = data.AddSlot("Main").Id;
            _ui = data.AddSlot("Ui").Id;
            data.SetInitialSlot(_main);

            var core = SceneRef.FromGuid("Core");
            data.SetCore(true, core);
            data.AddScene(core);
            data.AddScene(SceneRef.FromGuid("Managers"));
            data.AddScene(SceneRef.FromGuid("Gameplay"), SceneRef.FromGuid("Managers"));
            data.AddScene(SceneRef.FromGuid("Menu"), SceneRef.FromGuid("Managers"));
            data.AddScene(SceneRef.FromGuid("Hud"));
            var paths = new[] { "Core", "Managers", "Gameplay", "Menu", "Hud" }
                .Select(n => new ScenePathEntry { Guid = n, Path = Path(n), Name = n });

            _catalog = ScriptableObject.CreateInstance<SceneCatalog>();
            _catalog.Initialize(data, paths);

            _loader = new FakeSceneLoader();
            _manager = SceneManager.Inst;
            _manager.Configure(_catalog, _loader);
        }

        [TearDown]
        public void TearDown()
        {
            if (SceneManager.HasInstance)
                UnityEngine.Object.DestroyImmediate(SceneManager.Inst.gameObject);
            UnityEngine.Object.DestroyImmediate(_catalog);
        }

        private static IEnumerator Run(SceneOperation operation, float timeout = 5f)
        {
            operation.Perform();
            float end = Time.realtimeSinceStartup + timeout;
            while (!operation.IsFinished && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(operation.IsFinished, "The operation did not finish in time.");
        }

        private static SceneRef Scene(string name) => SceneRef.FromGuid(name);

        [UnityTest]
        public IEnumerator Load_LoadsCoreAndDependenciesBeforeScene()
        {
            var op = SceneAPI.Load(_main, Scene("Gameplay"));
            yield return Run(op);

            Assert.AreEqual(SceneOperationState.Completed, op.State, op.Error?.Message);
            CollectionAssert.AreEqual(new[] { "load:" + Path("Core"), "load:" + Path("Managers"), "load:" + Path("Gameplay") }, _loader.Log);
            Assert.AreEqual(3, _manager.LoadedScenes.Count);
            Assert.IsTrue(SceneAPI.TryGetSceneInSlot(_main, out LoadedSceneInfo info));
            Assert.AreEqual("Gameplay", info.Guid);
        }

        [UnityTest]
        public IEnumerator LoadedScenes_KnowTheirRole()
        {
            yield return Run(SceneAPI.Load(_main, Scene("Gameplay")));

            Assert.IsTrue(_manager.TryGetLoaded("Core", out LoadedSceneInfo core));
            Assert.IsTrue(core.IsCore);
            Assert.IsTrue(_manager.TryGetLoaded("Managers", out LoadedSceneInfo managers));
            Assert.IsTrue(managers.IsDependencyOnly);
        }

        [UnityTest]
        public IEnumerator SwapScene_KeepsSharedDependency()
        {
            yield return Run(SceneAPI.Load(_main, Scene("Gameplay")));
            _loader.Log.Clear();

            yield return Run(SceneAPI.Load(_main, Scene("Menu")));

            CollectionAssert.AreEqual(new[] { "unload:" + Path("Gameplay"), "load:" + Path("Menu") }, _loader.Log);
            Assert.IsTrue(SceneAPI.IsLoaded(Scene("Managers")));
        }

        [UnityTest]
        public IEnumerator UnloadSlot_UnloadsExclusiveDependencies_ButKeepsCore()
        {
            yield return Run(SceneAPI.Load(_main, Scene("Gameplay")));
            _loader.Log.Clear();

            yield return Run(SceneAPI.Unload(_main));

            CollectionAssert.AreEqual(new[] { "unload:" + Path("Gameplay"), "unload:" + Path("Managers") }, _loader.Log);
            Assert.IsTrue(SceneAPI.IsLoaded(Scene("Core")));
            Assert.IsFalse(SceneAPI.IsSlotOccupied(_main));
        }

        [UnityTest]
        public IEnumerator Operations_RunOneAtATime_InQueueOrder()
        {
            var finished = new List<string>();
            var first = SceneAPI.Load(_main, Scene("Gameplay"));
            var second = SceneAPI.Load(_main, Scene("Menu"));
            first.Finished += _ => finished.Add("first");
            second.Finished += _ => finished.Add("second");

            first.Perform();
            second.Perform();
            Assert.IsTrue(SceneAPI.IsBusy);
            Assert.AreEqual(1, _manager.QueuedCount);

            float end = Time.realtimeSinceStartup + 5f;
            while (!second.IsFinished && Time.realtimeSinceStartup < end) yield return null;

            CollectionAssert.AreEqual(new[] { "first", "second" }, finished);
            Assert.IsTrue(SceneAPI.TryGetSceneInSlot(_main, out LoadedSceneInfo info));
            Assert.AreEqual("Menu", info.Guid);
        }

        [UnityTest]
        public IEnumerator QueuedOperation_CanBeCancelled()
        {
            var first = SceneAPI.Load(_main, Scene("Gameplay"));
            var second = SceneAPI.Load(_main, Scene("Menu"));
            first.Perform();
            second.Perform();

            Assert.IsTrue(second.Cancel());
            Assert.AreEqual(SceneOperationState.Cancelled, second.State);

            float end = Time.realtimeSinceStartup + 5f;
            while (!first.IsFinished && Time.realtimeSinceStartup < end) yield return null;
            while (SceneAPI.IsBusy) yield return null;

            Assert.IsTrue(SceneAPI.TryGetSceneInSlot(_main, out LoadedSceneInfo info));
            Assert.AreEqual("Gameplay", info.Guid);
        }

        [UnityTest]
        public IEnumerator UnregisteredScene_FailsOperation_AndQueueKeepsRunning()
        {
            LogAssert.Expect(LogType.Error, new Regex("Scene operation failed"));

            var bad = SceneAPI.Load(_main, Scene("DoesNotExist"));
            var good = SceneAPI.Load(_main, Scene("Hud"));
            bad.Perform();
            good.Perform();

            float end = Time.realtimeSinceStartup + 5f;
            while (!good.IsFinished && Time.realtimeSinceStartup < end) yield return null;

            Assert.AreEqual(SceneOperationState.Failed, bad.State);
            Assert.IsNotNull(bad.Error);
            Assert.AreEqual(SceneOperationState.Completed, good.State);
        }

        [UnityTest]
        public IEnumerator Initializers_GateTheOperation()
        {
            var events = _loader.Log;
            _loader.Initializers[Path("Managers")] = new List<ISceneInitializer> { new TestInitializer { Name = "managers", Log = events, Frames = 4 } };
            _loader.Initializers[Path("Gameplay")] = new List<ISceneInitializer> { new TestInitializer { Name = "gameplay", Log = events, Frames = 2 } };

            var op = SceneAPI.Load(_main, Scene("Gameplay"));
            yield return Run(op);

            Assert.AreEqual(SceneOperationState.Completed, op.State, op.Error?.Message);

            // A dependency is fully initialized before the scene that needs it is loaded.
            Assert.Less(events.IndexOf("init-end:managers"), events.IndexOf("load:" + Path("Gameplay")));
            Assert.Less(events.IndexOf("load:" + Path("Gameplay")), events.IndexOf("init-start:gameplay"));
            Assert.Contains("init-end:gameplay", events);
        }

        [UnityTest]
        public IEnumerator Initializers_RunByOrder_AndInParallelWithinTheSameOrder()
        {
            var events = _loader.Log;
            _loader.Initializers[Path("Hud")] = new List<ISceneInitializer>
            {
                new TestInitializer { Name = "late", Log = events, InitializationOrder = 1, Frames = 1 },
                new TestInitializer { Name = "a", Log = events, InitializationOrder = 0, Frames = 3 },
                new TestInitializer { Name = "b", Log = events, InitializationOrder = 0, Frames = 3 },
            };

            yield return Run(SceneAPI.Load(_ui, Scene("Hud")));

            // Same order starts together...
            Assert.Less(events.IndexOf("init-start:b"), events.IndexOf("init-end:a"));
            // ...and the next order waits for all of them.
            Assert.Less(events.IndexOf("init-end:a"), events.IndexOf("init-start:late"));
            Assert.Less(events.IndexOf("init-end:b"), events.IndexOf("init-start:late"));
        }

        [UnityTest]
        public IEnumerator Payload_ReachesInitializers()
        {
            var initializer = new TestInitializer { Frames = 1 };
            _loader.Initializers[Path("Gameplay")] = new List<ISceneInitializer> { initializer };

            yield return Run(SceneAPI.Load(_main, Scene("Gameplay")).WithPayload("level-3"));

            Assert.AreEqual("level-3", initializer.ReceivedPayload);
        }

        [UnityTest]
        public IEnumerator FailingInitializer_FailsTheOperation()
        {
            LogAssert.ignoreFailingMessages = true;
            try
            {
                _loader.Initializers[Path("Hud")] = new List<ISceneInitializer> { new TestInitializer { Throw = true, Frames = 1 } };

                var op = SceneAPI.Load(_ui, Scene("Hud"));
                yield return Run(op);

                Assert.AreEqual(SceneOperationState.Failed, op.State);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [UnityTest]
        public IEnumerator InputIsBlockedWhileRunning_AndReleasedAfter()
        {
            _loader.Frames = 4;
            var op = SceneAPI.Load(_main, Scene("Hud"));
            op.Perform();
            yield return null;

            Assert.IsTrue(InputBlocker.IsBlocked);

            float end = Time.realtimeSinceStartup + 5f;
            while (!op.IsFinished && Time.realtimeSinceStartup < end) yield return null;
            yield return null;

            Assert.IsFalse(InputBlocker.IsBlocked);
        }

        [UnityTest]
        public IEnumerator InputBlocking_CanBeDisabled()
        {
            _loader.Frames = 4;
            var op = SceneAPI.Load(_main, Scene("Hud")).WithInputBlocking(false);
            op.Perform();
            yield return null;

            Assert.IsFalse(InputBlocker.IsBlocked);
            while (!op.IsFinished) yield return null;
        }

        [UnityTest]
        public IEnumerator Events_AreRaisedForLoadedScenes()
        {
            var loaded = new List<string>();
            Action<LoadedSceneInfo> handler = info => loaded.Add(info.Guid);
            SceneEvents.SceneLoaded += handler;
            try
            {
                yield return Run(SceneAPI.Load(_main, Scene("Gameplay")));
            }
            finally
            {
                SceneEvents.SceneLoaded -= handler;
            }

            CollectionAssert.AreEqual(new[] { "Core", "Managers", "Gameplay" }, loaded);
        }

        [UnityTest]
        public IEnumerator UnloadUnusedAssets_RunsWhenRequested()
        {
            yield return Run(SceneAPI.Load(_main, Scene("Hud")).WithUnloadUnusedAssets());
            Assert.Contains("unload-unused-assets", _loader.Log);
        }

        [UnityTest]
        public IEnumerator Reload_UnloadsAndLoadsSceneAgain()
        {
            yield return Run(SceneAPI.Load(_main, Scene("Hud")));
            _loader.Log.Clear();

            yield return Run(SceneAPI.Load(_main, Scene("Hud"), reload: true));

            CollectionAssert.AreEqual(new[] { "unload:" + Path("Hud"), "load:" + Path("Hud") }, _loader.Log);
        }

        [UnityTest]
        public IEnumerator OperationProgress_ReachesOne()
        {
            var op = SceneAPI.Load(_main, Scene("Gameplay"));
            yield return Run(op);
            Assert.AreEqual(1f, op.Progress, 0.0001f);
            Assert.AreEqual(SceneOperationPhase.Done, op.Phase);
        }
    }
}
