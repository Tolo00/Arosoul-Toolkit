using System.Collections.Generic;
using UnityEditor;

namespace Arosoul.Transitions.Editor
{
    [CustomPropertyDrawer(typeof(TransitionId))]
    public class TransitionIdDrawer : IdDropdownDrawer
    {
        protected override List<(int id, string label)> GetOptions()
        {
            var options = new List<(int id, string label)>();
            foreach (var definition in TransitionRegistryBuilder.FindDefinitions())
            {
                if (definition.Id.IsNone) continue;
                options.Add((definition.Id.Value, definition.Key));
            }
            return options;
        }
    }
}
