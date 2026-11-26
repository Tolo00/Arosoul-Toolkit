using System.Collections.Generic;
using Arosoul.GameInput;

namespace Arosoul.PlayerSettings {

    /// <summary>Adds one section per action map of the GameInput config. New maps show up in the layout window by themselves.</summary>
    public sealed class ActionMapSectionProvider : SettingSectionProvider {
        public override IEnumerable<SettingSection> GetSections() {
            var config = GameInputConfig.Load();
            if (config == null || config.Actions == null) yield break;

            foreach (var map in config.Actions.actionMaps) {
                if (!config.IsExcludedMap(map.name)) yield return new ActionMapSection(map.name);
            }
        }
    }

}
