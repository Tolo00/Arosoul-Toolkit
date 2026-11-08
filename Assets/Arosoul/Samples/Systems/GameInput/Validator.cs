namespace GameInputSystem
{
    internal static class Validator
    {
        public static void ValidateNotInFixedUpdate(string propertyName) {
            if (UnityEngine.Time.inFixedTimeStep) {
                UnityEngine.Debug.LogWarning(
                    $"[GameInput] {propertyName} accessed during FixedUpdate.\n" +
                    "This is unreliable! Do not use this property inside FixedUpdate."
                );
            }
        }
        

        // Log verbose messages (only if verbose logging is enabled)
        public static void LogVerbose(string message) {
            if (GameInput.VerboseLogging) {
                UnityEngine.Debug.Log($"[GameInput] {message}");
            }
        }
    }
}