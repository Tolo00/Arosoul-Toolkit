using System.Linq;
using Arosoul.Commands.Editor;
using NUnit.Framework;

namespace Arosoul.Commands.Tests
{
    public class CommandsEditorTests
    {
        [Test]
        public void Bake_KeepsRuntimeSettingsInSync()
        {
            CommandsSettingsBaker.Bake();
            CommandsRuntimeSettings runtime = CommandsRuntimeSettings.Instance;

            Assert.IsNotNull(runtime);
            Assert.IsTrue(CommandsSettingsBaker.IsInSync(CommandsSettings.instance.Data, runtime.Data));
            Assert.IsTrue(CommandsSettingsBaker.IsPreloaded(runtime));
        }

        [Test]
        public void Validate_HasNoErrors()
        {
            string[] errors = CommandsAgentTools.Validate().Where(d => d.StartsWith("ERROR")).ToArray();
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Execute_ReturnsOutputLines()
        {
            string[] output = CommandsAgentTools.Execute("echo hello there");
            CollectionAssert.AreEqual(new[] { "hello there" }, output);

            output = CommandsAgentTools.Execute("no-such-command");
            StringAssert.StartsWith("ERROR: Unknown command", output[0]);
        }

        [Test]
        public void Describe_ListsBuiltIns()
        {
            string description = CommandsAgentTools.Describe();
            StringAssert.Contains("Arosoul.Commands.BuiltInCommands", description);
            StringAssert.Contains("time-scale = <float>", description);
        }

        [Test]
        public void SourceLocator_FindsMemberLine()
        {
            CommandInfo help = CommandRegistry.Default.Find("help")[0];
            Assert.IsTrue(CommandSourceLocator.TryFind(help.Member, out var script, out int line));
            StringAssert.EndsWith("BuiltInCommands.cs", UnityEditor.AssetDatabase.GetAssetPath(script));

            string[] lines = System.IO.File.ReadAllLines(UnityEditor.AssetDatabase.GetAssetPath(script));
            StringAssert.Contains("Help(", lines[line - 1]);
        }
    }
}
