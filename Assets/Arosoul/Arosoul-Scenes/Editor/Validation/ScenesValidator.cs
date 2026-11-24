using System.Collections.Generic;
using Arosoul.Transitions.Editor;
using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    public enum BuildState
    {
        NotInBuild,
        Disabled,
        Enabled
    }

    /// <summary>
    /// What the validator needs to know about the project. Implemented by the editor, faked in tests.
    /// </summary>
    public interface ISceneProjectContext
    {
        bool SceneExists(string guid);
        BuildState GetBuildState(string guid);
    }

    public sealed class EditorSceneProjectContext : ISceneProjectContext
    {
        public bool SceneExists(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return !string.IsNullOrEmpty(path) && path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase);
        }

        public BuildState GetBuildState(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path != path) continue;
                return scene.enabled ? BuildState.Enabled : BuildState.Disabled;
            }
            return BuildState.NotInBuild;
        }
    }

    /// <summary>
    /// Checks Scenes configuration for mistakes. Pure logic apart from the project context.
    /// </summary>
    public static class ScenesValidator
    {
        public static ValidationReport Validate(ScenesData data, ISceneProjectContext context, bool buildSettingsAreSynced)
        {
            var report = new ValidationReport();

            ValidateSlots(data, report);
            ValidateCore(data, context, report);
            ValidateScenes(data, context, buildSettingsAreSynced, report);

            return report;
        }

        private static void ValidateSlots(ScenesData data, ValidationReport report)
        {
            if (data.Slots.Count == 0)
                report.Warning("No slots are defined. Add at least one slot to load scenes into.");

            var ids = new HashSet<int>();
            var keys = new List<string>();
            foreach (var slot in data.Slots)
            {
                if (slot.Id.IsNone) report.Error($"Slot '{slot.Key}' has no id.");
                else if (!ids.Add(slot.Id.Value)) report.Error($"Slot id {slot.Id.Value} is used more than once.");

                if (string.IsNullOrWhiteSpace(slot.Key)) report.Error($"Slot {slot.Id.Value} has an empty key.");
                keys.Add(slot.Key);
            }
            ReportDuplicateKeys(keys, "Slot", report);

            if (data.InitialSlot.IsNone)
                report.Error("No initial slot is set. It is the slot the first loaded scene occupies.");
            else if (!ids.Contains(data.InitialSlot.Value))
                report.Error("The initial slot does not exist.");
        }

        private static void ValidateCore(ScenesData data, ISceneProjectContext context, ValidationReport report)
        {
            if (!data.LoadCoreScene) return;

            if (!data.CoreScene.IsValid)
            {
                report.Error("The core scene is enabled but no scene is assigned.");
                return;
            }
            if (!context.SceneExists(data.CoreScene.Guid))
            {
                report.Error($"The core scene '{data.CoreScene.Name}' no longer exists.");
                return;
            }

            SceneDefinition definition = data.FindScene(data.CoreScene.Guid);
            if (definition != null && definition.Dependencies.Count > 0)
                report.Error("The core scene can not have dependencies, it is already a dependency of every scene.");
        }

        private static void ValidateScenes(ScenesData data, ISceneProjectContext context, bool synced, ValidationReport report)
        {
            var seen = new HashSet<string>();
            foreach (var definition in data.Scenes)
            {
                SceneRef scene = definition.Scene;
                if (!scene.IsValid)
                {
                    report.Error("A registered scene has no scene assigned.");
                    continue;
                }

                string label = LabelOf(scene);
                if (!seen.Add(scene.Guid))
                    report.Error($"Scene '{label}' is registered more than once.");

                if (!context.SceneExists(scene.Guid))
                {
                    report.Error($"Scene '{label}' no longer exists in the project.");
                }
                else if (!synced)
                {
                    BuildState state = context.GetBuildState(scene.Guid);
                    if (state == BuildState.NotInBuild) report.Warning($"Scene '{label}' is not in the build settings.");
                    else if (state == BuildState.Disabled) report.Warning($"Scene '{label}' is disabled in the build settings.");
                }

                var dependencies = new HashSet<string>();
                foreach (var dependency in definition.Dependencies)
                {
                    if (!dependency.IsValid)
                    {
                        report.Warning($"Scene '{label}' has an empty dependency entry.");
                        continue;
                    }

                    string dependencyLabel = LabelOf(dependency);
                    if (dependency.Guid == scene.Guid) report.Error($"Scene '{label}' depends on itself.");
                    else if (!dependencies.Add(dependency.Guid)) report.Warning($"Scene '{label}' lists dependency '{dependencyLabel}' more than once.");

                    if (!context.SceneExists(dependency.Guid)) report.Error($"Dependency '{dependencyLabel}' of scene '{label}' no longer exists.");
                    else if (!data.IsRegistered(dependency.Guid) && dependency.Guid != data.ActiveCoreGuid)
                        report.Warning($"Dependency '{dependencyLabel}' of scene '{label}' is not registered. It is loaded, but can not have dependencies of its own.");
                }
            }

            foreach (List<string> cycle in new SceneGraph(data).FindCycles())
                report.Error("Dependency cycle: " + string.Join(" -> ", cycle.ConvertAll(guid => NameOf(data, guid))));
        }

        // Keys become generated constant names, so keys that sanitize to the same name collide.
        private static void ReportDuplicateKeys(List<string> keys, string kind, ValidationReport report)
        {
            var seen = new HashSet<string>();
            var reported = new HashSet<string>();
            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                string name = IdentifierUtility.Sanitize(key);
                if (!seen.Add(name) && reported.Add(name))
                    report.Error($"{kind} key '{key}' collides with another {kind.ToLowerInvariant()} key (both become '{name}').");
            }
        }

        private static string LabelOf(SceneRef scene)
        {
            return scene.Name.Length > 0 ? scene.Name : scene.Guid;
        }

        private static string NameOf(ScenesData data, string guid)
        {
            SceneDefinition definition = data.FindScene(guid);
            return definition != null ? LabelOf(definition.Scene) : guid;
        }
    }
}
