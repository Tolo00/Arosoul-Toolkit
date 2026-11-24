using System.Collections.Generic;
using NUnit.Framework;

namespace Arosoul.Scenes.Tests
{
    public class ScenePlannerTests
    {
        private static readonly SlotId Main = new SlotId(1);
        private static readonly SlotId Ui = new SlotId(2);

        // Core <- Managers <- Gameplay, Managers <- MenuScene, Hud (no deps besides core)
        private static SceneGraph TestGraph(bool core = true)
        {
            var nodes = new List<KeyValuePair<string, string[]>>
            {
                new KeyValuePair<string, string[]>("Core", new string[0]),
                new KeyValuePair<string, string[]>("Managers", new string[0]),
                new KeyValuePair<string, string[]>("Gameplay", new[] { "Managers" }),
                new KeyValuePair<string, string[]>("Menu", new[] { "Managers" }),
                new KeyValuePair<string, string[]>("Hud", new string[0]),
            };
            return new SceneGraph(nodes, core ? "Core" : null);
        }

        private static ScenePlan Plan(SceneGraph graph, Dictionary<SlotId, string> slots, string[] loaded, SlotLoadRequest[] loads, SlotId[] unloads = null)
        {
            return ScenePlanner.Plan(graph, slots, loaded, loads, unloads ?? new SlotId[0]);
        }

        [Test]
        public void Load_FromEmpty_LoadsCoreAndDependenciesFirst()
        {
            var plan = Plan(TestGraph(), new Dictionary<SlotId, string>(), new string[0], new[] { new SlotLoadRequest(Main, "Gameplay") });

            Assert.IsTrue(plan.IsValid);
            CollectionAssert.AreEqual(new[] { "Core", "Managers", "Gameplay" }, plan.ToLoad);
            Assert.IsEmpty(plan.ToUnload);
            Assert.AreEqual("Gameplay", plan.FinalSlots[Main]);
        }

        [Test]
        public void SwapScene_KeepsSharedDependency()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay" }, new[] { new SlotLoadRequest(Main, "Menu") });

            CollectionAssert.AreEqual(new[] { "Gameplay" }, plan.ToUnload);
            CollectionAssert.AreEqual(new[] { "Menu" }, plan.ToLoad);
        }

        [Test]
        public void SwapScene_UnloadsDependencyNobodyNeedsAnymore()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay" }, new[] { new SlotLoadRequest(Main, "Hud") });

            // Dependents are unloaded before their dependencies.
            CollectionAssert.AreEqual(new[] { "Gameplay", "Managers" }, plan.ToUnload);
            CollectionAssert.AreEqual(new[] { "Hud" }, plan.ToLoad);
        }

        [Test]
        public void Unload_Slot_UnloadsSceneAndExclusiveDependencies_ButNeverCore()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay" }, new SlotLoadRequest[0], new[] { Main });

            CollectionAssert.AreEqual(new[] { "Gameplay", "Managers" }, plan.ToUnload);
            CollectionAssert.DoesNotContain(plan.ToUnload, "Core");
            Assert.IsEmpty(plan.ToLoad);
            Assert.IsFalse(plan.FinalSlots.ContainsKey(Main));
        }

        [Test]
        public void Unload_Slot_KeepsDependencyNeededByAnotherSlot()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" }, { Ui, "Menu" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay", "Menu" }, new SlotLoadRequest[0], new[] { Main });

            CollectionAssert.AreEqual(new[] { "Gameplay" }, plan.ToUnload);
        }

        [Test]
        public void LoadSameSceneIntoSameSlot_IsNoOp()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay" }, new[] { new SlotLoadRequest(Main, "Gameplay") });

            Assert.IsEmpty(plan.ToLoad);
            Assert.IsEmpty(plan.ToUnload);
        }

        [Test]
        public void Reload_UnloadsAndLoadsAgain()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Managers", "Gameplay" }, new[] { new SlotLoadRequest(Main, "Gameplay", reload: true) });

            CollectionAssert.AreEqual(new[] { "Gameplay" }, plan.ToUnload);
            CollectionAssert.AreEqual(new[] { "Gameplay" }, plan.ToLoad);
        }

        [Test]
        public void SceneInTwoSlots_IsAnError()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Hud" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Hud" }, new[] { new SlotLoadRequest(Ui, "Hud") });

            Assert.IsFalse(plan.IsValid);
        }

        [Test]
        public void MovingSceneBetweenSlots_InOneOperation_IsAllowed()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Hud" } };
            var plan = Plan(TestGraph(), slots, new[] { "Core", "Hud" }, new[] { new SlotLoadRequest(Ui, "Hud") }, new[] { Main });

            Assert.IsTrue(plan.IsValid);
            Assert.AreEqual("Hud", plan.FinalSlots[Ui]);
            Assert.IsEmpty(plan.ToLoad);
            Assert.IsEmpty(plan.ToUnload);
        }

        [Test]
        public void UnregisteredScene_IsAnError()
        {
            var plan = Plan(TestGraph(), new Dictionary<SlotId, string>(), new string[0], new[] { new SlotLoadRequest(Main, "Nope") });
            Assert.IsFalse(plan.IsValid);
        }

        [Test]
        public void InvalidSlot_IsAnError()
        {
            var plan = Plan(TestGraph(), new Dictionary<SlotId, string>(), new string[0], new[] { new SlotLoadRequest(SlotId.None, "Hud") });
            Assert.IsFalse(plan.IsValid);
        }

        [Test]
        public void WithoutCore_NothingExtraIsLoaded()
        {
            var plan = Plan(TestGraph(core: false), new Dictionary<SlotId, string>(), new string[0], new[] { new SlotLoadRequest(Main, "Hud") });
            CollectionAssert.AreEqual(new[] { "Hud" }, plan.ToLoad);
        }

        [Test]
        public void TwoSlotsInOneOperation_LoadSharedDependencyOnce()
        {
            var plan = Plan(TestGraph(), new Dictionary<SlotId, string>(), new string[0],
                new[] { new SlotLoadRequest(Main, "Gameplay"), new SlotLoadRequest(Ui, "Menu") });

            CollectionAssert.AreEqual(new[] { "Core", "Managers", "Gameplay", "Menu" }, plan.ToLoad);
        }

        [Test]
        public void EmptyOperation_LoadsMissingDependenciesOfAdoptedScene()
        {
            var slots = new Dictionary<SlotId, string> { { Main, "Gameplay" } };
            var plan = Plan(TestGraph(), slots, new[] { "Gameplay" }, new SlotLoadRequest[0]);

            CollectionAssert.AreEqual(new[] { "Core", "Managers" }, plan.ToLoad);
            Assert.IsEmpty(plan.ToUnload);
        }
    }
}
