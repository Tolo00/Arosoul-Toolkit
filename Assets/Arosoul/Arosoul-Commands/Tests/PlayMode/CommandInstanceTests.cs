using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arosoul.Commands.Tests
{
    public class CommandInstanceTests
    {
        private readonly List<GameObject> _created = new();
        private CommandRegistry _registry;
        private CommandLog _log;
        private CommandExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _registry = new CommandRegistry(new[] { typeof(TestTarget) });
            _log = new CommandLog();
            _executor = new CommandExecutor(_registry, _log);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        [UnityTest]
        public IEnumerator All_SetsEveryActiveInstance()
        {
            TestTarget a = Create("A");
            TestTarget b = Create("B");
            TestTarget inactive = Create("Inactive");
            inactive.gameObject.SetActive(false);
            yield return null;

            Assert.IsTrue(_executor.Execute("target.health = 50"));
            Assert.AreEqual(50f, a.Health);
            Assert.AreEqual(50f, b.Health);
            Assert.AreEqual(100f, inactive.Health, "Inactive objects are skipped.");
            StringAssert.EndsWith("on 2 objects", LastMessage());

            _log.Clear();
            Assert.IsTrue(_executor.Execute("target.health"));
            Assert.AreEqual(3, _log.Entries.Count, "Input, then one line per instance.");
            StringAssert.Contains(": 50", LastMessage());
        }

        [UnityTest]
        public IEnumerator First_RunsOnOneInstance()
        {
            TestTarget a = Create("A");
            TestTarget b = Create("B");
            yield return null;

            Assert.IsTrue(_executor.Execute("target.damage 30"));
            Assert.AreEqual(1, (a.Health < 100f ? 1 : 0) + (b.Health < 100f ? 1 : 0));
            Assert.AreEqual("70", LastMessage());
        }

        [UnityTest]
        public IEnumerator All_MethodWithResultPrintsPerInstance()
        {
            Create("A");
            Create("B");
            yield return null;

            Assert.IsTrue(_executor.Execute("target.describe"));
            Assert.AreEqual(3, _log.Entries.Count);
        }

        [UnityTest]
        public IEnumerator NoInstance_LogsWarning()
        {
            yield return null;

            Assert.IsFalse(_executor.Execute("target.damage 5"));
            Assert.AreEqual(CommandLogType.Warning, _log.Entries[_log.Entries.Count - 1].Type);
            StringAssert.Contains("No active TestTarget", LastMessage());
        }

        [UnityTest]
        public IEnumerator Console_OpensClosesAndRunsCommands()
        {
            var go = new GameObject("Console");
            _created.Add(go);
            var console = go.AddComponent<CommandConsole>();
            yield return null;

            Assert.AreSame(console, CommandConsole.Instance);
            Assert.IsFalse(console.IsOpen);

            console.Open();
            Assert.IsTrue(CommandAPI.IsConsoleOpen);

            Assert.IsTrue(console.Execute("echo from console"));
            Assert.AreEqual("from console", CommandAPI.Output.Entries[CommandAPI.Output.Entries.Count - 1].Message);
            Assert.AreEqual("echo from console", console.History.Entries[console.History.Entries.Count - 1]);

            console.Close();
            Assert.IsFalse(console.IsOpen);

            var duplicate = new GameObject("Duplicate").AddComponent<CommandConsole>();
            _created.Add(duplicate.gameObject);
            yield return null;
            Assert.IsTrue(duplicate == null, "A second console destroys itself.");
            Assert.AreSame(console, CommandConsole.Instance);
        }

        [UnityTest]
        public IEnumerator Console_PausesWhileOpenWhenEnabled()
        {
            CommandsSettingsData settings = CommandsRuntimeSettings.CurrentData;
            bool previous = settings.PauseWhileOpen;
            settings.PauseWhileOpen = true;
            try
            {
                var go = new GameObject("Console");
                _created.Add(go);
                var console = go.AddComponent<CommandConsole>();
                yield return null;

                console.Open();
                Assert.AreEqual(0f, Time.timeScale);
                console.Close();
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                settings.PauseWhileOpen = previous;
                Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator Console_MirrorsUnityLogs()
        {
            var go = new GameObject("Console");
            _created.Add(go);
            go.AddComponent<CommandConsole>();
            yield return null;

            CommandAPI.UnityLogLevel = UnityLogLevel.Warning;
            Debug.Log("plain log");
            Debug.LogWarning("mirrored warning");

            CommandLogEntry last = CommandAPI.Output.Entries[CommandAPI.Output.Entries.Count - 1];
            Assert.AreEqual("mirrored warning", last.Message);
            Assert.AreEqual(CommandLogType.Warning, last.Type);
        }

        private TestTarget Create(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<TestTarget>();
        }

        private string LastMessage() => _log.Entries[_log.Entries.Count - 1].Message;
    }
}
