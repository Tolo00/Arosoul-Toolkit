using MultiplayerInputSystem;
using UnityEngine;

/// <summary>
/// Simple bootloader used to initialize classes that inherit from SingletonPersistant<T>.
/// </summary>
public static class Bootloader {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() {
        InitializeSingletons();
    }

    private static void InitializeSingletons() {
        // _ = GameInput.Inst;
        // InputManager.Initialize();
    }
}