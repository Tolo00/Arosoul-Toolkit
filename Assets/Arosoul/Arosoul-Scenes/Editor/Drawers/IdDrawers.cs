using System.Collections.Generic;
using Arosoul.Transitions.Editor;
using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    [CustomPropertyDrawer(typeof(SlotId))]
    public class SlotIdDrawer : IdDropdownDrawer
    {
        protected override List<(int id, string label)> GetOptions()
        {
            var options = new List<(int id, string label)>();
            foreach (var slot in ScenesSettings.instance.Data.Slots)
                options.Add((slot.Id.Value, slot.Key));
            return options;
        }
    }
}
