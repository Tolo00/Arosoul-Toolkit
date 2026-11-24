using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arosoul.Transitions.Tests
{
    public class TransitionPlaybackTests
    {
        private FadeTransition _fade;
        private TransitionRegistry _registry;
        private static readonly TransitionId FadeId = new TransitionId(1);

        [SetUp]
        public void SetUp()
        {
            _fade = ScriptableObject.CreateInstance<FadeTransition>();
            SetId(_fade, FadeId.Value);

            _registry = ScriptableObject.CreateInstance<TransitionRegistry>();
            _registry.SetDefinitions(new List<TransitionDefinition> { _fade });
            TransitionRegistry.SetInstanceForTests(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            if (TransitionManager.HasInstance)
                Object.DestroyImmediate(TransitionManager.Inst.gameObject);

            TransitionRegistry.SetInstanceForTests(null);
            Object.DestroyImmediate(_registry);
            Object.DestroyImmediate(_fade);
        }

        // The id is normally assigned by the editor, the field is private.
        private static void SetId(TransitionDefinition definition, int id)
        {
            var field = typeof(TransitionDefinition).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(definition, id);
        }

        [UnityTest]
        public IEnumerator Play_RunsThroughAllPhases_AndBlocksInputWhileActive()
        {
            var handle = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.05f, 0.05f));
            var phases = new List<TransitionPhase>();
            handle.PhaseChanged += phases.Add;

            yield return null;
            Assert.IsTrue(InputBlocker.IsBlocked, "Input should be blocked while the transition is active.");

            float timeout = Time.realtimeSinceStartup + 5f;
            while (!handle.IsFinished && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.AreEqual(TransitionPhase.Completed, handle.Phase);
            CollectionAssert.Contains(phases, TransitionPhase.Hold);
            CollectionAssert.Contains(phases, TransitionPhase.Out);
            Assert.IsFalse(InputBlocker.IsBlocked, "Input should be released when the transition completes.");
        }

        [UnityTest]
        public IEnumerator HoldUntilReleased_StaysInHold_UntilReleased()
        {
            var handle = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.02f, 0.02f).WithHoldUntilReleased());

            float timeout = Time.realtimeSinceStartup + 5f;
            while (handle.Phase < TransitionPhase.Hold && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.AreEqual(TransitionPhase.Hold, handle.Phase);

            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(TransitionPhase.Hold, handle.Phase, "Should wait for release.");

            handle.Release();
            timeout = Time.realtimeSinceStartup + 5f;
            while (!handle.IsFinished && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.AreEqual(TransitionPhase.Completed, handle.Phase);
        }

        [UnityTest]
        public IEnumerator LowerPriority_DoesNotInterruptActiveTransition()
        {
            var first = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.5f).WithPriority(10).WithHoldUntilReleased());
            yield return null;

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("priority"));
            var second = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.1f).WithPriority(5));

            Assert.AreEqual(TransitionPhase.Cancelled, second.Phase);
            Assert.IsFalse(first.IsFinished);

            first.Cancel();
            Assert.AreEqual(TransitionPhase.Cancelled, first.Phase);
        }

        [UnityTest]
        public IEnumerator HigherPriority_InterruptsActiveTransition()
        {
            var first = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.5f).WithPriority(1).WithHoldUntilReleased());
            yield return null;

            var second = TransitionAPI.Play(TransitionParameters.Symmetric(FadeId, 0.5f).WithPriority(2).WithHoldUntilReleased());
            Assert.AreEqual(TransitionPhase.Cancelled, first.Phase);
            Assert.AreSame(second, TransitionAPI.GetActive());

            second.Cancel();
            yield return null;
            Assert.IsFalse(InputBlocker.IsBlocked);
        }
    }
}
