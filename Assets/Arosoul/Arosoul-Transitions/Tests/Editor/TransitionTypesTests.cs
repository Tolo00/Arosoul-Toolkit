using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Transitions.Tests
{
    public class TransitionIdTests
    {
        [Test]
        public void None_IsDefault()
        {
            Assert.IsTrue(TransitionId.None.IsNone);
            Assert.AreEqual(default(TransitionId), TransitionId.None);
        }

        [Test]
        public void Equality_IsByValue()
        {
            Assert.IsTrue(new TransitionId(3) == new TransitionId(3));
            Assert.IsTrue(new TransitionId(3) != new TransitionId(4));
            Assert.AreEqual(new TransitionId(3).GetHashCode(), new TransitionId(3).GetHashCode());
        }
    }

    public class TransitionParametersTests
    {
        [SetUp]
        public void SetUp()
        {
            TransitionRegistry.SetInstanceForTests(null);
        }

        [Test]
        public void Default_WithoutRegistry_UsesFallbackTimes()
        {
            var p = TransitionParameters.Default(new TransitionId(1));
            Assert.AreEqual(TransitionParameters.FallbackInTime, p.InTime);
            Assert.AreEqual(TransitionParameters.FallbackMinHoldTime, p.MinHoldTime);
            Assert.AreEqual(TransitionParameters.FallbackOutTime, p.OutTime);
        }

        [Test]
        public void With_ReturnsModifiedCopy()
        {
            var original = TransitionParameters.Symmetric(new TransitionId(1), 1f, 0.3f);
            var modified = original.WithPriority(5).WithHoldUntilReleased();

            Assert.AreEqual(0, original.Priority);
            Assert.IsFalse(original.HoldUntilReleased);
            Assert.AreEqual(5, modified.Priority);
            Assert.IsTrue(modified.HoldUntilReleased);
            Assert.AreEqual(1f, modified.OutTime);
        }

        [Test]
        public void ImplicitConversion_FromId()
        {
            TransitionParameters p = new TransitionId(2);
            Assert.AreEqual(2, p.Id.Value);
        }
    }

    public class InputBlockerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Make sure leftovers from other tests do not interfere.
            Assert.IsFalse(InputBlocker.IsBlocked);
        }

        [Test]
        public void Acquire_IsReferenceCounted()
        {
            var a = InputBlocker.Acquire("a");
            var b = InputBlocker.Acquire("b");
            Assert.IsTrue(InputBlocker.IsBlocked);
            Assert.AreEqual(2, InputBlocker.BlockCount);

            a.Dispose();
            Assert.IsTrue(InputBlocker.IsBlocked);

            b.Dispose();
            Assert.IsFalse(InputBlocker.IsBlocked);
        }

        [Test]
        public void Dispose_Twice_OnlyReleasesOnce()
        {
            var a = InputBlocker.Acquire("a");
            var b = InputBlocker.Acquire("b");
            a.Dispose();
            a.Dispose();
            Assert.AreEqual(1, InputBlocker.BlockCount);
            b.Dispose();
        }

        [Test]
        public void BlockedChanged_FiresOnFirstAcquireAndLastRelease()
        {
            int changes = 0;
            bool last = false;
            System.Action<bool> handler = blocked => { changes++; last = blocked; };
            InputBlocker.BlockedChanged += handler;

            var a = InputBlocker.Acquire();
            var b = InputBlocker.Acquire();
            Assert.AreEqual(1, changes);
            Assert.IsTrue(last);

            a.Dispose();
            Assert.AreEqual(1, changes);

            b.Dispose();
            Assert.AreEqual(2, changes);
            Assert.IsFalse(last);

            InputBlocker.BlockedChanged -= handler;
        }

        [Test]
        public void GetReasons_ListsActiveBlocks()
        {
            var a = InputBlocker.Acquire("first");
            var b = InputBlocker.Acquire("second");
            CollectionAssert.AreEqual(new[] { "first", "second" }, InputBlocker.GetReasons());
            a.Dispose();
            b.Dispose();
        }
    }

    public class DefaultTransitionTests
    {
        [Test]
        public void Fade_AppliesOpacityPerPhase()
        {
            var fade = ScriptableObject.CreateInstance<FadeTransition>();
            var visual = fade.Build();

            fade.Apply(visual, TransitionPhase.In, 0.25f);
            Assert.AreEqual(0.25f, visual.style.opacity.value, 0.0001f);

            fade.Apply(visual, TransitionPhase.Hold, 0.5f);
            Assert.AreEqual(1f, visual.style.opacity.value, 0.0001f);

            fade.Apply(visual, TransitionPhase.Out, 0.25f);
            Assert.AreEqual(0.75f, visual.style.opacity.value, 0.0001f);

            Object.DestroyImmediate(fade);
        }

        [Test]
        public void Evaluate_DefaultCurveIsMonotonic()
        {
            var fade = ScriptableObject.CreateInstance<FadeTransition>();
            Assert.AreEqual(0f, fade.Evaluate(0f), 0.0001f);
            Assert.AreEqual(1f, fade.Evaluate(1f), 0.0001f);
            Assert.Less(fade.Evaluate(0.2f), fade.Evaluate(0.8f));
            Object.DestroyImmediate(fade);
        }

        [Test]
        public void LoadingScreen_OnProgressUpdatesBarAndMessage()
        {
            var screen = ScriptableObject.CreateInstance<LoadingScreenTransition>();
            var visual = screen.Build();

            screen.OnProgress(visual, 0.5f, "Loading Gameplay");

            var label = visual.Q<Label>();
            Assert.AreEqual("Loading Gameplay", label.text);

            Object.DestroyImmediate(screen);
        }
    }
}
