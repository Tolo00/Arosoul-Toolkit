using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Arosoul.Commands.Editor
{
    /// <summary>
    /// Finds the script and line where a command is declared.
    /// </summary>
    public static class CommandSourceLocator
    {
        /// <summary>
        /// Open the script that declares the member in the code editor, at the member's line.
        /// </summary>
        public static bool TryOpen(MemberInfo member)
        {
            return TryFind(member, out MonoScript script, out int line) && AssetDatabase.OpenAsset(script, line);
        }

        /// <param name="line">1-based line of the member declaration, or 1 if it was not found in the file.</param>
        public static bool TryFind(MemberInfo member, out MonoScript script, out int line)
        {
            line = 1;
            script = member?.DeclaringType != null ? FindScript(member.DeclaringType) : null;
            if (script == null) return false;

            string[] lines = File.ReadAllLines(AssetDatabase.GetAssetPath(script));
            line = FindMemberLine(lines, member.Name) + 1;
            return true;
        }

        private static MonoScript FindScript(Type type)
        {
            Type outerType = type;
            while (outerType.DeclaringType != null) outerType = outerType.DeclaringType;

            // Scripts named after their class
            foreach (MonoScript script in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (script != null && script.GetClass() == outerType) return script;
            }

            // Scripts that declare the class without being named after it
            var declaration = new Regex($@"\b(class|struct)\s+{Regex.Escape(type.Name)}\b");
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                if (declaration.IsMatch(File.ReadAllText(path))) return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            }
            return null;
        }

        /// <returns>0-based line index.</returns>
        private static int FindMemberLine(string[] lines, string memberName)
        {
            var name = new Regex($@"\b{Regex.Escape(memberName)}\b");

            // The member right after a [Command] attribute
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("Command", StringComparison.Ordinal) < 0 || lines[i].IndexOf('[') < 0) continue;

                for (int j = i; j < lines.Length && j < i + 6; j++)
                {
                    string code = lines[j].TrimStart();
                    if (code.StartsWith("[")) code = code.Substring(code.LastIndexOf(']') + 1);
                    if (name.IsMatch(code)) return j;
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (name.IsMatch(lines[i])) return i;
            }
            return 0;
        }
    }
}
