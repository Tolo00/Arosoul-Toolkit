using System;
using UnityEngine;
using Arosoul.Scenes;


/// <summary>
/// Test helper for readiness gating. Put it in a scene that is loaded through Scenes (for example the one you load with SceneTester).
/// The operation waits for it before it continues. Logs the payload it receives.
/// </summary>
[AddComponentMenu("Arosoul/Scenes/Scene Initializer Tester")]
public class SceneInitializerTester : MonoBehaviour, ISceneInitializer
{
    [SerializeField] private int _order;
    [SerializeField, Min(0f)] private float _delay = 1f;
    [SerializeField] private bool _throwError;

    /// <inheritdoc />
    public int InitializationOrder => _order;

    /// <inheritdoc />
    public async Awaitable InitializeAsync(SceneContext context)
    {
        string payload = context.HasPayload ? context.Payload.ToString() : "none";
        Debug.Log($"SceneInitializerTester: '{context.Name}' started (slot {context.Slot}, order {_order}, payload {payload})", this);

        float elapsed = 0f;
        while (elapsed < _delay)
        {
            elapsed += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
        }

        if (_throwError)
            throw new InvalidOperationException("Test failure from SceneInitializerTester.");

        Debug.Log($"SceneInitializerTester: '{context.Name}' finished", this);
    }
}
