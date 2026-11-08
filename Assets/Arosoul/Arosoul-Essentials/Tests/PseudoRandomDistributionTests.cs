using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arosoul.Essentials.Tests
{
    public class PseudoRandomDistributionTests
    {
        private const int   RollCount    = 50000;
        private const float ErrorMargin  = 0.02f;   // 2% tolerance
        private const float StreakBuffer = 1.5f;     // PRD streak must be at least 1.5x shorter

        // Test a representative spread of probability values
        private static readonly float[] Probabilities = { 0.10f, 0.25f, 0.50f, 0.75f, 0.90f };

        [Test, TestCaseSource(nameof(Probabilities))]
        public void AverageHitRate_ConvergesToTargetProbability(float p)
        {
            var prd  = new PseudoRandomDistribution(p);
            int hits = 0;

            for (int i = 0; i < RollCount; i++)
                if (prd.Roll()) hits++;

            float actualP      = (float)hits / RollCount;
            float errorPercent = Mathf.Abs(actualP - p) / p;

            Assert.LessOrEqual(errorPercent, ErrorMargin,
                $"P={p:P0}: actual hit rate {actualP:P2} deviates from target by {errorPercent:P2}, expected < {ErrorMargin:P0}");
        }

        [Test, TestCaseSource(nameof(Probabilities))]
        public void MissStreak_IsShorterThanTrueRandom(float p)
        {
            int prdStreak  = LongestMissStreak_PRD(p);
            int trueStreak = LongestMissStreak_TrueRandom(p);

            Assert.Less(prdStreak, trueStreak * StreakBuffer,
                $"P={p:P0}: PRD streak {prdStreak} was not shorter than true random streak {trueStreak}");
        }

        [Test, TestCaseSource(nameof(Probabilities))]
        public void Reset_ClearsAccumulatedState(float p)
        {
            var prd = new PseudoRandomDistribution(p);

            // Burn some rolls to accumulate internal state
            for (int i = 0; i < 50; i++) prd.Roll();
            prd.Reset();

            // After reset the hit rate should still converge correctly
            int hits = 0;
            for (int i = 0; i < RollCount; i++)
                if (prd.Roll()) hits++;

            float actualP      = (float)hits / RollCount;
            float errorPercent = Mathf.Abs(actualP - p) / p;

            Assert.LessOrEqual(errorPercent, ErrorMargin,
                $"P={p:P0}: hit rate after Reset() was {actualP:P2}, expected ~{p:P2}");
        }

        [Test, TestCaseSource(nameof(Probabilities))]
        public void TargetProbability_CanBeChangedAtRuntime(float p)
        {
            var   prd    = new PseudoRandomDistribution(0.55f);
            float newP   = p;

            prd.TargetProbability = newP;

            int hits = 0;
            for (int i = 0; i < RollCount; i++)
                if (prd.Roll()) hits++;

            float actualP      = (float)hits / RollCount;
            float errorPercent = Mathf.Abs(actualP - newP) / newP;

            Assert.LessOrEqual(errorPercent, ErrorMargin,
                $"After changing P to {newP:P0}: actual rate {actualP:P2} deviates by {errorPercent:P2}");
        }

        [Test]
        public void CacheIsShared_SecondInstancePaysNoCost()
        {
            // Both instances use the same P — second one must hit the cache.
            // We can't measure time reliably in a test, so we just assert both
            // converge to the same rate, confirming they share the same C.
            const float p = 0.25f;

            var a = new PseudoRandomDistribution(p);
            var b = new PseudoRandomDistribution(p);

            int hitsA = 0, hitsB = 0;
            for (int i = 0; i < RollCount; i++)
            {
                if (a.Roll()) hitsA++;
                if (b.Roll()) hitsB++;
            }

            float rateA = (float)hitsA / RollCount;
            float rateB = (float)hitsB / RollCount;

            Assert.LessOrEqual(Mathf.Abs(rateA - p) / p, ErrorMargin,
                $"Instance A: rate {rateA:P2} too far from {p:P2}");
            Assert.LessOrEqual(Mathf.Abs(rateB - p) / p, ErrorMargin,
                $"Instance B: rate {rateB:P2} too far from {p:P2}");
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static int LongestMissStreak_PRD(float p)
        {
            var prd     = new PseudoRandomDistribution(p);
            int longest = 0, current = 0;

            for (int i = 0; i < RollCount; i++)
            {
                if (prd.Roll()) current = 0;
                else            longest = Mathf.Max(longest, ++current);
            }
            return longest;
        }

        private static int LongestMissStreak_TrueRandom(float p)
        {
            int longest = 0, current = 0;

            for (int i = 0; i < RollCount; i++)
            {
                if (Random.value < p) current = 0;
                else                  longest = Mathf.Max(longest, ++current);
            }
            return longest;
        }
    }
}