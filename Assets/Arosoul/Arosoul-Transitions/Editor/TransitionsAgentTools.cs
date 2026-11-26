using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Public editor API for creating, inspecting and validating transitions without driving the Transitions window.
    /// Call it from an editor script, a batch mode -executeMethod entry point or an automation bridge.
    /// Every method that changes assets refreshes the registry and the generated TransitionIds before it returns.
    /// </summary>
    public static class TransitionsAgentTools
    {
        /// <summary>
        /// Full names of every non-abstract <see cref="TransitionDefinition"/> type that can be created.
        /// </summary>
        public static string[] GetDefinitionTypeNames()
        {
            return GetDefinitionTypes().Select(t => t.FullName).ToArray();
        }

        /// <summary>
        /// All transition definitions in the project, ordered by asset path.
        /// </summary>
        public static List<TransitionDefinition> FindDefinitions()
        {
            return TransitionRegistryBuilder.FindDefinitions();
        }

        /// <summary>
        /// Find a definition by its key. Returns null if no definition uses the key.
        /// </summary>
        public static TransitionDefinition FindDefinition(string key)
        {
            return FindDefinitions().FirstOrDefault(d => d.Key == key);
        }

        /// <summary>
        /// Create a definition asset and assign its id.
        /// </summary>
        /// <param name="assetName">File name of the asset, without extension. Made unique if it is taken.</param>
        /// <param name="key">Name of the generated TransitionIds member. Defaults to the sanitized asset name.</param>
        public static T CreateDefinition<T>(string assetName, string key = null) where T : TransitionDefinition
        {
            return (T)CreateDefinition(typeof(T), assetName, key);
        }

        /// <summary>
        /// Create a definition asset of a type found by its name or full name, see <see cref="GetDefinitionTypeNames"/>.
        /// </summary>
        public static TransitionDefinition CreateDefinition(string typeName, string assetName, string key = null)
        {
            Type type = GetDefinitionTypes().FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
            if (type == null)
                throw new ArgumentException($"No non-abstract TransitionDefinition type named '{typeName}'. Available: {string.Join(", ", GetDefinitionTypeNames())}.", nameof(typeName));

            return CreateDefinition(type, assetName, key);
        }

        /// <summary>
        /// Create a definition asset of the given type and assign its id.
        /// </summary>
        public static TransitionDefinition CreateDefinition(Type type, string assetName, string key = null)
        {
            if (type == null || type.IsAbstract || !typeof(TransitionDefinition).IsAssignableFrom(type))
                throw new ArgumentException("Type must be a non-abstract TransitionDefinition.", nameof(type));
            if (string.IsNullOrWhiteSpace(assetName))
                throw new ArgumentException("Asset name can not be empty.", nameof(assetName));

            TransitionDefinition definition = TransitionRegistryBuilder.CreateDefinition(type, assetName.Trim());
            if (!string.IsNullOrWhiteSpace(key))
                ApplyKey(definition, key);

            AssetDatabase.SaveAssets();
            RefreshOrThrow();
            return definition;
        }

        /// <summary>
        /// Change the key of a definition, which renames its generated TransitionIds member. The id is kept.
        /// </summary>
        public static void SetKey(TransitionDefinition definition, string key)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key can not be empty.", nameof(key));

            ApplyKey(definition, key);
            AssetDatabase.SaveAssets();
            RefreshOrThrow();
        }

        /// <summary>
        /// Delete a definition asset. Serialized references to its id stop resolving.
        /// </summary>
        /// <returns>True if the asset was deleted.</returns>
        public static bool DeleteDefinition(TransitionDefinition definition)
        {
            if (definition == null) return false;

            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.DeleteAsset(path)) return false;

            RefreshOrThrow();
            return true;
        }

        /// <summary>
        /// Assign ids, write the registry and regenerate TransitionIds.cs.
        /// </summary>
        /// <returns>The asset path of the generated ids file.</returns>
        public static string Regenerate()
        {
            RefreshOrThrow();
            return TransitionRegistryBuilder.IdsPath;
        }

        /// <summary>
        /// Diagnostics of the transition setup. Entries prefixed with ERROR must be fixed, WARNING entries are advisory.
        /// An empty array means the setup is valid.
        /// </summary>
        public static string[] Validate()
        {
            var diagnostics = new List<string>();
            List<TransitionDefinition> definitions = FindDefinitions();

            if (definitions.Count == 0)
                diagnostics.Add("WARNING: The project has no transition definitions.");

            foreach (var group in definitions.GroupBy(d => IdentifierUtility.Sanitize(d.Key)).Where(g => g.Count() > 1))
                diagnostics.Add($"WARNING: Keys {string.Join(", ", group.Select(d => $"'{d.Key}'"))} generate the same member name '{group.Key}'; the duplicates get numeric suffixes. Give each definition a unique key.");

            foreach (TransitionDefinition definition in definitions)
            {
                if (definition.Id.IsNone)
                    diagnostics.Add($"ERROR: '{AssetDatabase.GetAssetPath(definition)}' has no id. Run TransitionsAgentTools.Regenerate().");
            }
            foreach (var group in definitions.Where(d => !d.Id.IsNone).GroupBy(d => d.Id.Value).Where(g => g.Count() > 1))
                diagnostics.Add($"ERROR: Id {group.Key} is used by {string.Join(", ", group.Select(d => $"'{d.Key}'"))}. Run TransitionsAgentTools.Regenerate().");

            var registry = AssetDatabase.LoadAssetAtPath<TransitionRegistry>(TransitionRegistryBuilder.RegistryPath);
            if (registry == null)
            {
                diagnostics.Add($"ERROR: No registry at '{TransitionRegistryBuilder.RegistryPath}'. Run TransitionsAgentTools.Regenerate().");
            }
            else
            {
                if (!registry.Definitions.SequenceEqual(definitions))
                    diagnostics.Add("ERROR: The registry does not match the definitions in the project. Run TransitionsAgentTools.Regenerate().");
                if (!PlayerSettings.GetPreloadedAssets().Contains(registry))
                    diagnostics.Add("ERROR: The registry is not a preloaded asset, transitions can not be found in builds. Run TransitionsAgentTools.Regenerate().");
            }

            if (TransitionRegistryBuilder.IsIdsOutOfSync())
                diagnostics.Add($"ERROR: '{TransitionRegistryBuilder.IdsPath}' is out of sync with the definitions. Run TransitionsAgentTools.Regenerate().");

            return diagnostics.ToArray();
        }

        /// <summary>
        /// Readable summary of every definition (key, id, type, default times, input blocking and asset path).
        /// </summary>
        public static string Describe()
        {
            var sb = new StringBuilder();
            List<TransitionDefinition> definitions = FindDefinitions();
            var names = IdentifierUtility.MakeUnique(definitions.Select(d => d.Key).ToList());

            sb.Append("Transitions (").Append(definitions.Count).Append("):\n");
            for (int i = 0; i < definitions.Count; i++)
            {
                TransitionDefinition d = definitions[i];
                sb.Append($"- TransitionIds.{names[i]} id={d.Id.Value} type={d.GetType().Name} ")
                  .Append($"in={d.DefaultInTime} hold={d.DefaultMinHoldTime} out={d.DefaultOutTime} blockInput={d.BlockInput} ")
                  .Append($"path={AssetDatabase.GetAssetPath(d)}\n");
            }
            return sb.ToString();
        }

        private static IEnumerable<Type> GetDefinitionTypes()
        {
            return TypeCache.GetTypesDerivedFrom<TransitionDefinition>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .OrderBy(t => t.FullName);
        }

        private static void ApplyKey(TransitionDefinition definition, string key)
        {
            var so = new SerializedObject(definition);
            so.FindProperty("_key").stringValue = key.Trim();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        private static void RefreshOrThrow()
        {
            if (!TransitionRegistryBuilder.RefreshIgnoringCompilation())
                throw new InvalidOperationException("The transition registry can not be refreshed while playing or importing assets. It is scheduled for the next editor update; call Regenerate() again once the editor is idle.");
        }
    }
}
