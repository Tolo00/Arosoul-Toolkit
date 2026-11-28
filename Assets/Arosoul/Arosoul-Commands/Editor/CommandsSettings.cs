using UnityEditor;
using UnityEngine;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Project wide console settings, stored in ProjectSettings/CommandsSettings.asset and edited in
    /// Tools > Arosoul Commands > Settings. The runtime reads a baked copy, see <see cref="CommandsRuntimeSettings"/>.
    /// </summary>
    [FilePath("ProjectSettings/CommandsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class CommandsSettings : ScriptableSingleton<CommandsSettings>
    {
        [SerializeField] CommandsSettingsData _data = new();

        public CommandsSettingsData Data => _data;

        public void SaveSettings()
        {
            Save(true);
        }

        /// <summary>
        /// A serialized object for editing the settings in UI. The singleton is not editable by default.
        /// </summary>
        internal SerializedObject CreateSerializedObject()
        {
            hideFlags &= ~HideFlags.NotEditable;
            return new SerializedObject(this);
        }
    }
}
