using System;
using System.Collections.Generic;

namespace Arosoul.Scenes
{
    public readonly struct SlotLoadRequest
    {
        public readonly SlotId Slot;
        public readonly string Guid;
        public readonly bool Reload;

        public SlotLoadRequest(SlotId slot, string guid, bool reload = false)
        {
            Slot = slot;
            Guid = guid;
            Reload = reload;
        }
    }

    /// <summary>
    /// The result of planning an operation: which scenes to unload and load, and how the slots look afterwards.
    /// </summary>
    public sealed class ScenePlan
    {
        /// <summary>
        /// Scenes to unload. Dependents come before their dependencies.
        /// </summary>
        public readonly List<string> ToUnload = new List<string>();

        /// <summary>
        /// Scenes to load. Dependencies come before their dependents.
        /// </summary>
        public readonly List<string> ToLoad = new List<string>();

        /// <summary>
        /// Slot contents after the operation. Only slots that hold a scene are included.
        /// </summary>
        public readonly Dictionary<SlotId, string> FinalSlots = new Dictionary<SlotId, string>();

        public readonly List<string> Errors = new List<string>();

        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>
    /// Works out what has to be loaded and unloaded to get from the current state to the requested one.
    /// A loaded scene is kept as long as it is in a slot, is the core scene, or is a dependency of a kept scene.
    /// Pure logic, no Unity scene APIs.
    /// </summary>
    public static class ScenePlanner
    {
        public static ScenePlan Plan(
            SceneGraph graph,
            IReadOnlyDictionary<SlotId, string> currentSlots,
            IReadOnlyCollection<string> loaded,
            IReadOnlyList<SlotLoadRequest> loads,
            IReadOnlyList<SlotId> unloads)
        {
            var plan = new ScenePlan();

            // 1. Apply slot changes
            foreach (var kvp in currentSlots)
                plan.FinalSlots[kvp.Key] = kvp.Value;

            for (int i = 0; i < unloads.Count; i++)
                plan.FinalSlots.Remove(unloads[i]);

            var reload = new HashSet<string>();
            for (int i = 0; i < loads.Count; i++)
            {
                var request = loads[i];
                if (request.Slot.IsNone)
                {
                    plan.Errors.Add("A scene was requested for an invalid slot.");
                    continue;
                }
                if (string.IsNullOrEmpty(request.Guid) || !graph.Contains(request.Guid))
                {
                    plan.Errors.Add($"Scene '{request.Guid}' is not registered in Scene Config.");
                    continue;
                }

                plan.FinalSlots[request.Slot] = request.Guid;
                if (request.Reload) reload.Add(request.Guid);
            }

            // A scene can only live in one slot.
            var slotOfScene = new Dictionary<string, SlotId>();
            foreach (var kvp in plan.FinalSlots)
            {
                if (slotOfScene.TryGetValue(kvp.Value, out SlotId other))
                    plan.Errors.Add($"Scene '{kvp.Value}' would be in two slots at once ({other} and {kvp.Key}).");
                else
                    slotOfScene[kvp.Value] = kvp.Key;
            }
            if (!plan.IsValid) return plan;

            // 2. Which scenes are needed afterwards
            var roots = new List<string>();
            if (graph.CoreGuid != null) roots.Add(graph.CoreGuid);

            var slotIds = new List<SlotId>(plan.FinalSlots.Keys);
            slotIds.Sort((a, b) => a.Value.CompareTo(b.Value));
            foreach (var slot in slotIds) roots.Add(plan.FinalSlots[slot]);

            if (!graph.TryResolveLoadOrder(roots, out List<string> needed, out List<string> cycle))
            {
                plan.Errors.Add("Scene dependency cycle: " + string.Join(" -> ", cycle));
                return plan;
            }
            var neededSet = new HashSet<string>(needed);

            // 3. Unload everything loaded that is not needed, plus scenes that are reloaded
            var unloadSet = new HashSet<string>();
            foreach (string guid in loaded)
            {
                if (!neededSet.Contains(guid) || reload.Contains(guid))
                    unloadSet.Add(guid);
            }

            if (unloadSet.Count > 0)
            {
                var sortedRoots = new List<string>(unloadSet);
                sortedRoots.Sort(StringComparer.Ordinal);
                graph.TryResolveLoadOrder(sortedRoots, out List<string> unloadOrder, out _);
                for (int i = unloadOrder.Count - 1; i >= 0; i--)
                    if (unloadSet.Contains(unloadOrder[i])) plan.ToUnload.Add(unloadOrder[i]);

                // Cycles are reported by validation, make sure nothing is dropped if the order could not be resolved.
                foreach (string guid in sortedRoots)
                    if (!plan.ToUnload.Contains(guid)) plan.ToUnload.Add(guid);
            }

            // 4. Load everything needed that is not already loaded
            var stillLoaded = new HashSet<string>(loaded);
            stillLoaded.ExceptWith(unloadSet);
            foreach (string guid in needed)
            {
                if (!stillLoaded.Contains(guid))
                    plan.ToLoad.Add(guid);
            }

            return plan;
        }
    }
}
