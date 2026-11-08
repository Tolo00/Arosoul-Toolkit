
namespace Arosoul.SceneSystem
{
    public class TransitionParameters
    {
        public string TransitionKey;

        public float InTime;
        public float OutTime;
        public float MinWaitTime;

        public TransitionParameters(string transitionKey, float fadeTime, float minWaitTime)
        {
            TransitionKey = transitionKey;
            InTime = fadeTime;
            MinWaitTime = minWaitTime;
            OutTime = fadeTime;
        }

        public TransitionParameters(string transitionKey, float fadeTime)
        {
            TransitionKey = transitionKey;
            InTime = fadeTime;
            MinWaitTime = 0.5f;
            OutTime = fadeTime;
        }

        public TransitionParameters(string transitionKey, float inTime, float minWaitTime, float outTime)
        {
            TransitionKey = transitionKey;
            InTime = inTime;
            MinWaitTime = minWaitTime;
            OutTime = outTime;
        }

        public static TransitionParameters FastFade => new TransitionParameters("Fade", 0.5f, 0.2f);
        public static TransitionParameters SlowFade => new TransitionParameters("Fade", 1f, 0.5f);
    }
}