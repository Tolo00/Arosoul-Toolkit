using UnityEngine;

namespace Arosoul.GameInput {

    internal static class Validator {
        public static bool Verbose;

        public static void ValidateNotInFixedUpdate(string propertyName) {
            if (Time.inFixedTimeStep) {
                Debug.LogWarning(
                    $"[GameInput] {propertyName} accessed during FixedUpdate.\n" +
                    "This is unreliable! Do not use this property inside FixedUpdate.");
            }
        }

        public static void LogVerbose(string message) {
            if (Verbose) Debug.Log($"[GameInput] {message}");
        }
    }

    internal static class NameUtil {
        /// <summary>"leftStick" => "Left Stick", "up" => "Up".</summary>
        public static string Nicify(string id) {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            var sb = new System.Text.StringBuilder(id.Length + 4);
            for (int i = 0; i < id.Length; i++) {
                char c = id[i];
                if (c == '_' || c == '-') { sb.Append(' '); continue; }
                if (i > 0 && char.IsUpper(c) && (char.IsLower(id[i - 1]) || char.IsDigit(id[i - 1]))) sb.Append(' ');
                sb.Append(sb.Length == 0 ? char.ToUpperInvariant(c) : c);
            }
            return sb.ToString();
        }
    }

}
