using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Public editor API for inspecting commands, changing console settings and running commands without the editor windows.
    /// Call it from an editor script, a batch mode -executeMethod entry point or an automation bridge.
    /// </summary>
    public static class CommandsAgentTools
    {
        /// <summary>
        /// Diagnostics of the command setup and settings. Entries prefixed with ERROR must be fixed, WARNING entries are advisory.
        /// An empty array means the setup is valid.
        /// </summary>
        public static string[] Validate()
        {
            var diagnostics = new List<string>();
            foreach (CommandIssue issue in CommandRegistry.Default.Issues)
                diagnostics.Add(issue.ToString());

            CommandsSettingsData data = CommandsSettings.instance.Data;
            var runtime = AssetDatabase.LoadAssetAtPath<CommandsRuntimeSettings>(CommandsSettingsBaker.RuntimeSettingsPath);
            if (runtime == null)
            {
                diagnostics.Add($"ERROR: No runtime settings at '{CommandsSettingsBaker.RuntimeSettingsPath}'. Run CommandsAgentTools.Bake().");
            }
            else
            {
                if (!CommandsSettingsBaker.IsInSync(data, runtime.Data))
                    diagnostics.Add("ERROR: The runtime settings are out of sync with the project settings. Run CommandsAgentTools.Bake().");
                if (!CommandsSettingsBaker.IsPreloaded(runtime))
                    diagnostics.Add("ERROR: The runtime settings are not a preloaded asset, builds use the default settings. Run CommandsAgentTools.Bake().");
            }

            if (data.ToggleKey == Key.None)
                diagnostics.Add("WARNING: The toggle key is None, the console can only be opened from code.");

            foreach (IGrouping<Key, string> group in GetKeyBindings(data).Where(b => b.key != Key.None).GroupBy(b => b.key, b => b.name).Where(g => g.Count() > 1))
                diagnostics.Add($"WARNING: {string.Join(" and ", group)} share the key {group.Key}; only the first in Toggle, Close, Submit, Autocomplete, Previous, Next order works.");

            if (data.PanelSettings == null && data.Theme == null)
                diagnostics.Add("WARNING: No theme is set, console text does not render in builds. Run CommandsAgentTools.Bake() to find the default runtime theme, or set one in the settings.");

            if (CommandsConsoleCreator.FindPrefab() == null)
                diagnostics.Add($"WARNING: The '{CommandsConsoleCreator.PrefabName}' prefab was not found; GameObject > Arosoul > Commands Console creates a plain object instead.");

            return diagnostics.ToArray();
        }

        /// <summary>
        /// Readable summary of the settings and every command, grouped by declaring class.
        /// </summary>
        public static string Describe()
        {
            var sb = new StringBuilder(DescribeSettings());
            CommandRegistry registry = CommandRegistry.Default;

            sb.Append($"\nCommands ({registry.Commands.Count}):\n");
            foreach (IGrouping<Type, CommandInfo> group in registry.Commands.GroupBy(c => c.DeclaringType).OrderBy(g => g.Key.FullName))
            {
                sb.Append($"{group.Key.FullName} ({group.Key.Assembly.GetName().Name}):\n");
                foreach (CommandInfo command in group)
                {
                    sb.Append($"  - {command.Signature} [{command.Kind}, {command.TargetDescription}, member {command.Member.Name}]");
                    if (command.Description.Length > 0) sb.Append($" {command.Description}");
                    sb.Append('\n');
                }
            }

            if (registry.Issues.Count > 0)
            {
                sb.Append($"\nIssues ({registry.Issues.Count}):\n");
                foreach (CommandIssue issue in registry.Issues)
                    sb.Append($"  - {issue}\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Readable summary of the console settings.
        /// </summary>
        public static string DescribeSettings()
        {
            CommandsSettingsData d = CommandsSettings.instance.Data;
            return "Settings:\n" +
                   $"  availability={d.Availability} dontDestroyOnLoad={d.DontDestroyOnLoad}\n" +
                   $"  keys toggle={d.ToggleKey} close={d.CloseKey} submit={d.SubmitKey} autocomplete={d.AutocompleteKey} previous={d.PreviousKey} next={d.NextKey}\n" +
                   $"  pauseWhileOpen={d.PauseWhileOpen} unityLogLevel={d.UnityLogLevel} showStackTraces={d.ShowStackTraces}\n" +
                   $"  maxSuggestions={d.MaxSuggestions} maxLogEntries={d.MaxLogEntries} historySize={d.HistorySize} persistHistory={d.PersistHistory}\n" +
                   $"  height={d.Height} fontSize={d.FontSize} sortingOrder={d.SortingOrder} panelSettings={NameOf(d.PanelSettings)} theme={NameOf(d.Theme)}\n";
        }

        /// <summary>
        /// Commands matching the query, ranked like the console's autocomplete.
        /// </summary>
        public static CommandInfo[] FindCommands(string query, int maxResults = 20)
        {
            return CommandAutocomplete.GetSuggestions(CommandRegistry.Default, query, maxResults).ToArray();
        }

        /// <summary>
        /// Change the settings, save them and bake the runtime settings.
        /// </summary>
        /// <example><c>CommandsAgentTools.ModifySettings(s => s.ToggleKey = Key.F1);</c></example>
        public static void ModifySettings(Action<CommandsSettingsData> change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));

            CommandsSettings settings = CommandsSettings.instance;
            change(settings.Data);
            settings.SaveSettings();
            CommandsSettingsBaker.Bake();
            CommandsSettingsWindow.RefreshOpenWindows();
        }

        /// <summary>
        /// Write the settings to the runtime settings asset and make sure it is preloaded.
        /// </summary>
        /// <returns>The asset path of the runtime settings.</returns>
        public static string Bake()
        {
            CommandsSettingsBaker.Bake();
            return CommandsSettingsBaker.RuntimeSettingsPath;
        }

        /// <summary>
        /// Run console input, in edit mode or play mode, and return the lines it logged (prefixed with WARNING: or ERROR: where relevant).
        /// Instance commands only find objects in the loaded scenes.
        /// </summary>
        public static string[] Execute(string input)
        {
            var log = new CommandLog();
            new CommandExecutor(CommandRegistry.Default, log).Execute(input);

            return log.Entries
                .Where(e => e.Type != CommandLogType.Input)
                .Select(e => e.Type switch
                {
                    CommandLogType.Warning => $"WARNING: {e.Message}",
                    CommandLogType.Error => $"ERROR: {e.Message}",
                    _ => e.Message
                })
                .ToArray();
        }

        /// <summary>
        /// Add the Commands Console prefab to the active scene, or return the existing console. The scene is not saved.
        /// </summary>
        public static GameObject AddConsoleToActiveScene()
        {
            return CommandsConsoleCreator.AddConsoleToActiveScene();
        }

        private static IEnumerable<(string name, Key key)> GetKeyBindings(CommandsSettingsData data)
        {
            yield return ("Toggle", data.ToggleKey);
            yield return ("Close", data.CloseKey);
            yield return ("Submit", data.SubmitKey);
            yield return ("Autocomplete", data.AutocompleteKey);
            yield return ("Previous", data.PreviousKey);
            yield return ("Next", data.NextKey);
        }

        private static string NameOf(UnityEngine.Object asset) => asset != null ? asset.name : "none";
    }
}
