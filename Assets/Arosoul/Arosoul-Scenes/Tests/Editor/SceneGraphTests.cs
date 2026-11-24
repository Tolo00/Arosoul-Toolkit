using System.Collections.Generic;
using NUnit.Framework;

namespace Arosoul.Scenes.Tests
{
    public class SceneGraphTests
    {
        private static SceneGraph Graph(string core, params (string scene, string[] deps)[] nodes)
        {
            var list = new List<KeyValuePair<string, string[]>>();
            foreach (var node in nodes) list.Add(new KeyValuePair<string, string[]>(node.scene, node.deps));
            return new SceneGraph(list, core);
        }

        [Test]
        public void ResolveLoadOrder_PutsDependenciesFirst()
        {
            var graph = Graph(null, ("A", new[] { "B" }), ("B", new[] { "C" }), ("C", new string[0]));
            CollectionAssert.AreEqual(new[] { "C", "B", "A" }, graph.ResolveLoadOrder(new[] { "A" }));
        }

        [Test]
        public void ResolveLoadOrder_LoadsSharedDependencyOnce()
        {
            var graph = Graph(null, ("A", new[] { "C" }), ("B", new[] { "C" }), ("C", new string[0]));
            var order = graph.ResolveLoadOrder(new[] { "A", "B" });
            CollectionAssert.AreEqual(new[] { "C", "A", "B" }, order);
        }

        [Test]
        public void Core_IsImplicitDependencyOfEveryScene()
        {
            var graph = Graph("Core", ("Core", new string[0]), ("A", new[] { "B" }), ("B", new string[0]));
            CollectionAssert.AreEqual(new[] { "Core", "B", "A" }, graph.ResolveLoadOrder(new[] { "A" }));
        }

        [Test]
        public void Core_DoesNotDependOnItself()
        {
            var graph = Graph("Core", ("Core", new string[0]));
            CollectionAssert.AreEqual(new[] { "Core" }, graph.ResolveLoadOrder(new[] { "Core" }));
            Assert.IsEmpty(graph.GetDependencies("Core"));
        }

        [Test]
        public void Core_IsNotDuplicatedWhenListedExplicitly()
        {
            var graph = Graph("Core", ("Core", new string[0]), ("A", new[] { "Core" }));
            CollectionAssert.AreEqual(new[] { "Core" }, graph.GetDependencies("A"));
        }

        [Test]
        public void TryResolveLoadOrder_DetectsCycle()
        {
            var graph = Graph(null, ("A", new[] { "B" }), ("B", new[] { "C" }), ("C", new[] { "A" }));
            bool ok = graph.TryResolveLoadOrder(new[] { "A" }, out _, out List<string> cycle);

            Assert.IsFalse(ok);
            Assert.AreEqual(cycle[0], cycle[cycle.Count - 1]);
            CollectionAssert.IsSubsetOf(new[] { "A", "B", "C" }, cycle);
        }

        [Test]
        public void ResolveLoadOrder_ThrowsOnCycle()
        {
            var graph = Graph(null, ("A", new[] { "B" }), ("B", new[] { "A" }));
            Assert.Throws<System.InvalidOperationException>(() => graph.ResolveLoadOrder(new[] { "A" }));
        }

        [Test]
        public void FindCycles_ReportsEachCycleOnce()
        {
            var graph = Graph(null, ("A", new[] { "B" }), ("B", new[] { "A" }), ("C", new string[0]));
            Assert.AreEqual(1, graph.FindCycles().Count);
        }

        [Test]
        public void FindCycles_NoCycles_ReturnsEmpty()
        {
            var graph = Graph(null, ("A", new[] { "B" }), ("B", new string[0]));
            Assert.IsEmpty(graph.FindCycles());
        }

        [Test]
        public void GetDependents_ReturnsDirectDependents()
        {
            var graph = Graph(null, ("A", new[] { "C" }), ("B", new[] { "C" }), ("C", new string[0]));
            CollectionAssert.AreEqual(new[] { "A", "B" }, graph.GetDependents("C"));
        }

        [Test]
        public void UnknownScene_IsResolvedWithoutDependencies()
        {
            var graph = Graph(null, ("A", new[] { "Unregistered" }));
            CollectionAssert.AreEqual(new[] { "Unregistered", "A" }, graph.ResolveLoadOrder(new[] { "A" }));
            Assert.IsFalse(graph.Contains("Unregistered"));
        }
    }
}
