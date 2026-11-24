using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arosoul.Scenes.Tests
{
    /// <summary>
    /// Loads nothing. Records what was asked and takes a configurable number of frames, so Scenes can be tested without real scenes.
    /// </summary>
    internal sealed class FakeSceneLoader : ISceneLoader
    {
        public readonly List<string> Log = new List<string>();
        public readonly Dictionary<string, List<ISceneInitializer>> Initializers = new Dictionary<string, List<ISceneInitializer>>();
        public int Frames = 1;
        public int SetActiveCalls;

        public async Awaitable<Scene> LoadAdditiveAsync(string path, Action<float> onProgress)
        {
            for (int i = 0; i < Frames; i++)
            {
                onProgress?.Invoke((float)i / Frames);
                await Awaitable.NextFrameAsync();
            }
            onProgress?.Invoke(1f);
            Log.Add("load:" + path);
            return default;
        }

        public async Awaitable UnloadAsync(Scene scene, string path)
        {
            await Awaitable.NextFrameAsync();
            Log.Add("unload:" + path);
        }

        public void SetActive(Scene scene)
        {
            SetActiveCalls++;
        }

        public async Awaitable UnloadUnusedAssetsAsync()
        {
            await Awaitable.NextFrameAsync();
            Log.Add("unload-unused-assets");
        }

        public List<ISceneInitializer> GetInitializers(Scene scene, string path)
        {
            return Initializers.TryGetValue(path, out List<ISceneInitializer> list) ? list : new List<ISceneInitializer>();
        }
    }

    internal sealed class TestInitializer : ISceneInitializer
    {
        public int InitializationOrder { get; set; }
        public int Frames = 3;
        public string Name = "init";
        public List<string> Log;
        public bool Throw;
        public object ReceivedPayload;

        public async Awaitable InitializeAsync(SceneContext context)
        {
            Log?.Add($"init-start:{Name}");
            ReceivedPayload = context.Payload;
            for (int i = 0; i < Frames; i++)
                await Awaitable.NextFrameAsync();
            if (Throw) throw new InvalidOperationException("Initializer failed on purpose");
            Log?.Add($"init-end:{Name}");
        }
    }
}
