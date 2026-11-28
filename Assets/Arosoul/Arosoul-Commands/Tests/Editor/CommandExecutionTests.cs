using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Arosoul.Commands.Tests
{
    public class CommandExecutionTests
    {
        private CommandRegistry _registry;
        private CommandLog _log;
        private CommandExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            TestCommands.Reset();
            _registry = new CommandRegistry(new[] { typeof(TestCommands), typeof(TestCommands.Nested) });
            _log = new CommandLog();
            _executor = new CommandExecutor(_registry, _log);
        }

        #region Discovery

        [Test]
        public void Discovery_NamesCommandsWithPrefixAndKebabCase()
        {
            Assert.IsTrue(_registry.Contains("test.number"));
            Assert.IsTrue(_registry.Contains("test.secret"));
            Assert.IsTrue(_registry.Contains("test.echo-upper"));
            Assert.IsTrue(_registry.Contains("test.set-mode"));
            Assert.IsTrue(_registry.Contains("test.nested.value"));
            Assert.IsTrue(_registry.Contains("TEST.NUMBER"), "Lookup is case-insensitive.");
            Assert.AreEqual(3, _registry.Find("test.overload").Count);
            Assert.IsEmpty(_registry.Issues);
        }

        [Test]
        public void Discovery_DescribesSignatures()
        {
            Assert.AreEqual("test.add <int a> [int b = 10]", _registry.Find("test.add")[0].Signature);
            Assert.AreEqual("test.number = <int>", _registry.Find("test.number")[0].Signature);
            Assert.AreEqual("test.read-only-number : int (read-only)", _registry.Find("test.read-only-number")[0].Signature);
        }

        [Test]
        public void Discovery_ReportsInvalidMembers()
        {
            var registry = new CommandRegistry(new[] { typeof(InvalidCommands) });
            string[] messages = registry.Issues.Select(i => i.ToString()).ToArray();

            AssertIssue(messages, "ERROR", "InstanceOnPlainClass", "not a Component");
            AssertIssue(messages, "ERROR", "has space", "invalid command name");
            AssertIssue(messages, "ERROR", "RefParameter", "ref, in or out");
            AssertIssue(messages, "ERROR", "UnsupportedParameter", "unsupported type object");
            AssertIssue(messages, "WARNING", "UnsupportedProperty", "can be read but not set");
            AssertIssue(messages, "ERROR", "'dup'", "several properties or fields");

            Assert.IsFalse(registry.Contains("instance-on-plain-class"));
            Assert.IsTrue(registry.Contains("unsupported-property"));
            Assert.IsFalse(registry.Find("unsupported-property")[0].CanWrite);
        }

        [Test]
        public void DefaultRegistry_ContainsBuiltInsButNotTestCommands()
        {
            Assert.IsTrue(CommandRegistry.Default.Contains("help"));
            Assert.IsTrue(CommandRegistry.Default.Contains("time-scale"));
            Assert.IsFalse(CommandRegistry.Default.Contains("test.number"));
        }

        #endregion

        #region Values

        [Test]
        public void Assignment_SetsFieldsAndProperties()
        {
            Assert.IsTrue(_executor.Execute("test.number = 5"));
            Assert.AreEqual(5, TestCommands.Number);

            Assert.IsTrue(_executor.Execute("test.speed = 2.5"));
            Assert.AreEqual(2.5f, TestCommands.Speed);

            Assert.IsTrue(_executor.Execute("test.position = 1 2 3"));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), TestCommands.Position);

            Assert.IsTrue(_executor.Execute("test.tint = cyan"));
            Assert.AreEqual(Color.cyan, TestCommands.Tint);

            Assert.IsTrue(_executor.Execute("test.mode = no-clip"));
            Assert.AreEqual(TestMode.NoClip, TestCommands.Mode);

            Assert.IsTrue(_executor.Execute("test.text = \"hello world\""));
            Assert.AreEqual("hello world", TestCommands.Text);

            Assert.IsTrue(_executor.Execute("test.nested.value = 9"));
            Assert.AreEqual(9, TestCommands.Nested.Value);
            Assert.AreEqual("test.nested.value = 9", LastMessage());
        }

        [Test]
        public void Assignment_WithoutEqualsSignIsAShorthand()
        {
            Assert.IsTrue(_executor.Execute("test.number 12"));
            Assert.AreEqual(12, TestCommands.Number);
        }

        [Test]
        public void Read_PrintsValue()
        {
            Assert.IsTrue(_executor.Execute("test.secret"));
            Assert.AreEqual("5", LastMessage());

            TestCommands.Position = new Vector3(1f, 2f, 3f);
            Assert.IsTrue(_executor.Execute("test.position"));
            Assert.AreEqual("(1, 2, 3)", LastMessage());
        }

        [Test]
        public void Assignment_Errors()
        {
            Assert.IsFalse(_executor.Execute("test.read-only-number = 3"));
            StringAssert.Contains("read-only", LastMessage());

            Assert.IsFalse(_executor.Execute("test.number = abc"));
            StringAssert.Contains("Could not parse 'abc' as int", LastMessage());

            Assert.IsFalse(_executor.Execute("test.mode = flying"));
            StringAssert.Contains("Values: Normal, GodMode, NoClip", LastMessage());

            Assert.IsFalse(_executor.Execute("test.number ="));
            StringAssert.Contains("Missing value", LastMessage());

            Assert.IsFalse(_executor.Execute("test.add = 3"));
            StringAssert.Contains("is a method", LastMessage());
            Assert.AreEqual(CommandLogType.Error, _log.Entries[_log.Entries.Count - 1].Type);
        }

        #endregion

        #region Methods

        [Test]
        public void Method_PrintsReturnValue()
        {
            Assert.IsTrue(_executor.Execute("test.echo-upper abc"));
            Assert.AreEqual("ABC", LastMessage());
        }

        [Test]
        public void Method_UsesDefaultValuesForOptionalParameters()
        {
            Assert.IsTrue(_executor.Execute("test.add 1"));
            Assert.AreEqual("11", LastMessage());

            Assert.IsTrue(_executor.Execute("test.add 1 2"));
            Assert.AreEqual("3", LastMessage());

            Assert.IsTrue(_executor.Execute("test.set-mode"));
            Assert.AreEqual(TestMode.GodMode, TestCommands.Mode);
        }

        [Test]
        public void Method_ChoosesOverloadByArgumentCount()
        {
            Assert.IsTrue(_executor.Execute("test.overload"));
            Assert.AreEqual("none", LastMessage());

            Assert.IsTrue(_executor.Execute("test.overload 4"));
            Assert.AreEqual("int 4", LastMessage());

            Assert.IsTrue(_executor.Execute("test.overload 4 5"));
            Assert.AreEqual("two 4 5", LastMessage());
        }

        [Test]
        public void Method_TrailingStringTakesTheRestOfTheInput()
        {
            Assert.IsTrue(_executor.Execute("test.echo-upper hello big world"));
            Assert.AreEqual("HELLO BIG WORLD", LastMessage());

            Assert.IsTrue(_executor.Execute("test.record \"big goblin\" 3"));
            Assert.AreEqual("big goblin:3", TestCommands.LastCall);
        }

        [Test]
        public void Method_Errors()
        {
            Assert.IsFalse(_executor.Execute("test.add"));
            StringAssert.Contains("Wrong number of arguments", LastMessage());

            Assert.IsFalse(_executor.Execute("test.add x"));
            StringAssert.Contains("Could not parse 'x' as int for parameter 'a'", LastMessage());

            Assert.IsFalse(_executor.Execute("test.fail"));
            StringAssert.Contains("InvalidOperationException: Boom", LastMessage());
            Assert.IsNotNull(_log.Entries[_log.Entries.Count - 1].StackTrace);
        }

        [Test]
        public void UnknownCommand_SuggestsClosestMatch()
        {
            Assert.IsFalse(_executor.Execute("test.numbr = 3"));
            StringAssert.Contains("Unknown command 'test.numbr'", LastMessage());
            StringAssert.Contains("Did you mean 'test.number'?", LastMessage());
        }

        [Test]
        public void Execute_LogsInputFirst()
        {
            _executor.Execute("test.number = 1");
            Assert.AreEqual(CommandLogType.Input, _log.Entries[0].Type);
            Assert.AreEqual("test.number = 1", _log.Entries[0].Message);
        }

        #endregion

        #region Autocomplete

        [Test]
        public void Autocomplete_RanksPrefixBeforeFuzzy()
        {
            var suggestions = CommandAutocomplete.GetSuggestions(_registry, "test.num", 5);
            Assert.AreEqual("test.number", suggestions[0].Name);

            suggestions = CommandAutocomplete.GetSuggestions(_registry, "echo", 5);
            Assert.AreEqual("test.echo-upper", suggestions[0].Name, "Segment matches are found.");

            suggestions = CommandAutocomplete.GetSuggestions(_registry, "tnv", 5);
            Assert.AreEqual("test.nested.value", suggestions[0].Name, "Fuzzy matches prefer segment starts.");

            Assert.IsEmpty(CommandAutocomplete.GetSuggestions(_registry, "zzz", 5));
        }

        [Test]
        public void Autocomplete_ScoreOrder()
        {
            int exact = CommandAutocomplete.Score("time-scale", "time-scale");
            int prefix = CommandAutocomplete.Score("time-scale", "time");
            int segment = CommandAutocomplete.Score("time-scale", "scale");
            int contains = CommandAutocomplete.Score("time-scale", "cal");
            int fuzzy = CommandAutocomplete.Score("time-scale", "tsc");

            Assert.Greater(exact, prefix);
            Assert.Greater(prefix, segment);
            Assert.Greater(segment, contains);
            Assert.Greater(contains, fuzzy);
            Assert.Greater(fuzzy, 0);
        }

        #endregion

        #region History

        [Test]
        public void History_BrowsesAndRestoresDraft()
        {
            var history = new CommandHistory(3);
            history.Add("a");
            history.Add("b");
            history.Add("c");
            history.Add("d");
            history.Add("c");
            CollectionAssert.AreEqual(new[] { "b", "d", "c" }, history.Entries, "Capacity is kept and repeats move to the end.");

            Assert.IsTrue(history.TryGetPrevious("draft", out string command));
            Assert.AreEqual("c", command);
            Assert.IsTrue(history.TryGetPrevious("ignored", out command));
            Assert.AreEqual("d", command);
            Assert.IsTrue(history.TryGetNext(out command));
            Assert.AreEqual("c", command);
            Assert.IsTrue(history.TryGetNext(out command));
            Assert.AreEqual("draft", command);
            Assert.IsFalse(history.TryGetNext(out _));
        }

        #endregion

        private string LastMessage() => _log.Entries[_log.Entries.Count - 1].Message;

        private static void AssertIssue(string[] messages, string severity, string member, string text)
        {
            bool found = messages.Any(m => m.StartsWith(severity) && m.Contains(member) && m.Contains(text));
            Assert.IsTrue(found, $"Expected {severity} about {member} containing '{text}'. Issues:\n{string.Join("\n", messages)}");
        }
    }
}
