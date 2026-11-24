using UnityEditor;

namespace Arosoul.Scenes.Editor
{
    /// <summary>
    /// Applies serialized changes as one undo step with a readable name.
    /// </summary>
    internal static class ScenesUndo
    {
        public static bool Apply(SerializedObject serializedObject, string name)
        {
            // Start a fresh group so the name only applies to this change.
            Undo.IncrementCurrentGroup();
            bool changed = serializedObject.ApplyModifiedProperties();

            // Only rename if something was recorded, otherwise the previous step would be renamed.
            if (changed) Undo.SetCurrentGroupName(name);
            return changed;
        }
    }
}