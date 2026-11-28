using UnityEngine;

namespace Arosoul.Commands
{
    /// <summary>
    /// Entry point for running commands and writing to the console from code.
    /// </summary>
    public static class CommandAPI
    {
        private static CommandExecutor _executor;

        /// <summary>
        /// Every command in the project.
        /// </summary>
        public static CommandRegistry Registry => CommandRegistry.Default;

        /// <summary>
        /// The log shown by the in-game console.
        /// </summary>
        public static CommandLog Output => CommandLog.Default;

        /// <summary>
        /// Minimum level of Unity log messages mirrored into the console. Starts at the project setting, the log-level command changes it.
        /// </summary>
        public static UnityLogLevel UnityLogLevel { get; set; } = UnityLogLevel.Warning;

        public static bool IsConsoleOpen => CommandConsole.Instance != null && CommandConsole.Instance.IsOpen;

        /// <summary>
        /// Run console input as if it was typed, for example "player.weapon.damage = 3".
        /// </summary>
        /// <returns>True if the command ran without errors.</returns>
        public static bool Execute(string input)
        {
            _executor ??= new CommandExecutor(Registry, Output);
            return _executor.Execute(input);
        }

        public static void Log(string message) => Output.Add(message);

        public static void LogWarning(string message) => Output.Add(message, CommandLogType.Warning);

        public static void LogError(string message) => Output.Add(message, CommandLogType.Error);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            UnityLogLevel = CommandsRuntimeSettings.CurrentData.UnityLogLevel;
        }
    }
}
