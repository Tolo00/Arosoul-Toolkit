using System;
using Arosoul.Essentials;
using UnityEngine;

/// <summary>
/// Static wrapper around the Time class.
/// Prefered over interacting with Time directly as it can keep track of whether the game is paused or not.
/// </summary>
public static class TimeService {
    private static float currentTimeScale = 1;
    private static bool isPaused = false;

    public static float DeltaTime => Time.deltaTime;
    public static float UnscaledDeltaTime => Time.unscaledDeltaTime;
    public static float TimeScale => Time.timeScale;

    public static bool IsPaused => isPaused;


    public static event Action OnTimePaused;
    public static event Action OnTimeResumed;

    public static event Action<DataChange<float>> OnSetTimeScale;


    public static void Pause() {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0;

        OnTimePaused?.Invoke();
    }

    public static void Resume() {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = currentTimeScale;

        OnTimeResumed?.Invoke();
    }

    public static void SetTimeScale(float value) {
        float oldTimeScale = TimeScale;
        float newTimeScale = value;

        currentTimeScale = newTimeScale;
        if (!isPaused) Time.timeScale = newTimeScale;

        OnSetTimeScale?.Invoke(new DataChange<float>(oldTimeScale, newTimeScale));
    }
}
