using System;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arosoul.Commands
{
    /// <summary>
    /// Commands that ship with the console.
    /// </summary>
    internal static class BuiltInCommands
    {
        [Command("help", "Shows how to use the console, or the details of a command.")]
        private static string Help(string command = null)
        {
            return string.IsNullOrWhiteSpace(command) ? GetUsage() : GetCommandHelp(command.Trim());
        }

        [Command("clear", "Clears the console log.")]
        private static void Clear()
        {
            CommandAPI.Output.Clear();
        }

        [Command("commands", "Lists every command, optionally only those whose name or description contains the filter.")]
        private static string ListCommands(string filter = null)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (CommandInfo command in CommandAPI.Registry.Commands)
            {
                bool matches = string.IsNullOrWhiteSpace(filter) ||
                               command.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               command.Description.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matches) continue;

                sb.Append(command.Signature);
                if (command.Description.Length > 0) sb.Append("  -  ").Append(command.Description);
                sb.Append('\n');
                count++;
            }

            sb.Append(count == 1 ? "1 command." : $"{count} commands.");
            return sb.ToString();
        }

        [Command("echo", "Prints the text.")]
        private static string Echo(string text)
        {
            return text;
        }

        [Command("time-scale", "Speed of game time. 1 is normal speed, 0 pauses.")]
        private static float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = Mathf.Max(0f, value);
        }

        [Command("target-frame-rate", "Frame rate the game tries to run at. -1 uses the platform default.")]
        private static int TargetFrameRate
        {
            get => Application.targetFrameRate;
            set => Application.targetFrameRate = value;
        }

        [Command("fps", "Current frames per second, smoothed.")]
        private static float Fps => Time.smoothDeltaTime > 0f ? Mathf.Round(1f / Time.smoothDeltaTime) : 0f;

        [Command("log-level", "Minimum level of Unity log messages shown in the console: None, Error, Warning or Log.")]
        private static UnityLogLevel LogLevel
        {
            get => CommandAPI.UnityLogLevel;
            set => CommandAPI.UnityLogLevel = value;
        }

        [Command("quit", "Quits the game, or exits play mode in the editor.")]
        private static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static string GetUsage()
        {
            CommandsSettingsData settings = CommandsRuntimeSettings.CurrentData;
            return "Type a command and press " + settings.SubmitKey + " to run it.\n" +
                   $"  {settings.AutocompleteKey} completes the selected suggestion. {settings.PreviousKey}/{settings.NextKey} select a suggestion, or browse the history when none are shown.\n" +
                   "  Call a method:   spawn goblin 3 (1, 2, 0)    (quotes for text with spaces, parentheses for vectors)\n" +
                   "  Read a value:    player.health\n" +
                   "  Set a value:     player.health = 100\n" +
                   "Type 'commands' to list every command, or 'help <command>' for details.";
        }

        private static string GetCommandHelp(string name)
        {
            var matches = CommandAPI.Registry.Find(name);
            if (matches.Count == 0)
            {
                CommandInfo closest = CommandAutocomplete.GetSuggestions(CommandAPI.Registry, name, 1).FirstOrDefault();
                return closest != null ? $"Unknown command '{name}'. Did you mean '{closest.Name}'?" : $"Unknown command '{name}'.";
            }

            var sb = new StringBuilder();
            foreach (CommandInfo command in matches)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(command.Signature).Append('\n');
                if (command.Description.Length > 0) sb.Append("  ").Append(command.Description).Append('\n');
                sb.Append($"  {command.Kind}, {command.TargetDescription}, declared in {command.MemberPath}");
            }
            return sb.ToString();
        }
    }
}
