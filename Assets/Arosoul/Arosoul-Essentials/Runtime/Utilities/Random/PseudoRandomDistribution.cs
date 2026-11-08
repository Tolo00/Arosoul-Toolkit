using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pseudo-Random Distribution sampler, using the same technique as Dota 2.
/// The average hit rate converges to the target probability P, but streaks
/// are heavily suppressed — you can't get 10 misses in a row on a 25% chance.
/// </summary>
public class PseudoRandomDistribution
{
    // Shared across ALL instances — computed once per unique P value ever used
    private static readonly Dictionary<int, float> _cCache = new();

    private float _c;
    private float _currentP;

    public float Attempts { get; private set; }

    private float _targetProbability;
    public float TargetProbability
    {
        get => _targetProbability;
        set
        {
            _targetProbability = Mathf.Clamp01(value);
            _targetProbability = Mathf.Round(_targetProbability * 100f); // Store the probability in int format to avoid floating point error
            _c = GetOrComputeC((int)_targetProbability);
            Reset();
        }
    }

    public PseudoRandomDistribution(float targetProbability)
    {
        TargetProbability = targetProbability;
    }

    private static float GetOrComputeC(int p)
    {
        if (_cCache.TryGetValue(p, out float cached))
            return cached;

        float c = SolveC((float)p / 100f);
        _cCache[p] = c;
        return c;
    }

    // Call this once from a GameManager or loading screen to precalculate the values
    public static void PrewarmCache(float min = 0.05f, float max = 0.95f, int steps = 19)
    {
        for (int i = 0; i <= steps; i++)
        {
            float p = Mathf.Lerp(min, max, (float)i / steps);
            int pInt = Mathf.RoundToInt(p * 100f);   // snap to nearest whole number, ensure no floating point inaccuracy is stored
            GetOrComputeC(pInt);
        }
        Debug.Log($"[PRD] Cache pre-warmed with {_cCache.Count} entries.");
    }

    /// <summary>
    /// Roll the PRD. Returns true on a hit.
    /// </summary>
    public bool Roll()
    {
        Attempts++;
        bool hit = Random.value < _currentP;

        if (hit)
            _currentP = _c;          // reset after success
        else
            _currentP = Mathf.Min(_currentP + _c, 1f);  // grow on failure

        return hit;
    }

    /// <summary>
    /// Reset the internal state (e.g. between rounds or after a player respawns).
    /// </summary>
    public void Reset()
    {
        _currentP = _c;
        Attempts = 0;
    }

    private static float SolveC(float p)
    {
        if (p <= 0f) return 0f;
        if (p >= 1f) return 1f;

        float lo = 0f, hi = p;

        // Binary search is simpler and more robust than Newton-Raphson here
        for (int i = 0; i < 64; i++)
        {
            float mid = (lo + hi) * 0.5f;
            if (ExpectedHitRate(mid) < p)
                lo = mid;
            else
                hi = mid;

            if (hi - lo < 1e-7f) break;
        }

        return (lo + hi) * 0.5f;
    }

    private static float ExpectedHitRate(float c)
    {
        if (c <= 0f) return 0f;
        if (c >= 1f) return 1f;

        // E[hit rate] = sum(P(hit on attempt n)) / sum(n * P(hit on attempt n))
        // P(hit on attempt n) = min(n*c, 1) * product of (1 - min(k*c,1)) for k=1..n-1
        float pMiss       = 1f;
        float sumHitProb  = 0f;   // sum of P(hit on exactly attempt n)
        float sumExpected = 0f;   // sum of n * P(hit on exactly attempt n)
        int   maxN        = Mathf.CeilToInt(1f / c);

        for (int n = 1; n <= maxN; n++)
        {
            float pn   = Mathf.Min(n * c, 1f) * pMiss;
            sumHitProb  += pn;
            sumExpected += n * pn;
            pMiss       *= 1f - Mathf.Min(n * c, 1f);
            if (pMiss < 1e-9f) break;
        }

        // Average hit rate = expected hits per cycle / expected attempts per cycle
        // = 1 / sumExpected (since sumHitProb converges to 1)
        return (sumHitProb > 0f && sumExpected > 0f) ? sumHitProb / sumExpected : 0f;
    }
}