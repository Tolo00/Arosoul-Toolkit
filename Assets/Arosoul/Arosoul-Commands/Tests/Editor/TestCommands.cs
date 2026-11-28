using System;
using UnityEngine;

namespace Arosoul.Commands.Tests
{
    public enum TestMode
    {
        Normal,
        GodMode,
        NoClip
    }

    [Flags]
    public enum TestFlags
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4
    }

    /// <summary>
    /// Static commands used by the editor tests. Test assemblies are not scanned, so these never show up in the console.
    /// </summary>
    [CommandPrefix("test")]
    internal static class TestCommands
    {
        [Command] public static int Number;
        [Command] private static int _secret = 5;

        private static float _speed = 1f;
        [Command] public static readonly int ReadOnlyNumber = 7;
        [Command] public static Vector3 Position;
        [Command] public static Color Tint;
        [Command] public static TestMode Mode;
        [Command] public static string Text;

        public static string LastCall;

        [Command("echo-upper")]
        public static string EchoUpper(string text) => text.ToUpperInvariant();

        [Command]
        public static int Add(int a, int b = 10) => a + b;

        [Command]
        public static string Overload() => "none";

        [Command]
        public static string Overload(int value) => $"int {value}";

        [Command]
        public static string Overload(int a, int b) => $"two {a} {b}";

        [Command]
        public static void SetMode(TestMode mode = TestMode.GodMode) => Mode = mode;

        [Command]
        public static void Fail() => throw new InvalidOperationException("Boom");

        [Command]
        public static void Record(string name, int count)
        {
            LastCall = $"{name}:{count}";
        }

        [Command]
        public static float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        [CommandPrefix("nested")]
        internal static class Nested
        {
            [Command] public static int Value;
        }

        public static void Reset()
        {
            Number = 0;
            _speed = 1f;
            _secret = 5;
            Position = Vector3.zero;
            Tint = Color.clear;
            Mode = TestMode.Normal;
            Text = null;
            LastCall = null;
            Nested.Value = 0;
        }
    }

    /// <summary>
    /// Members with setup problems, for the discovery tests.
    /// </summary>
    internal class InvalidCommands
    {
        [Command] public int InstanceOnPlainClass;

        [Command("has space")]
        public static void BadName() { }

        [Command]
        public static void RefParameter(ref int value) { }

        [Command]
        public static void UnsupportedParameter(object value) { }

        [Command]
        public static object UnsupportedProperty { get; set; }

        [Command("dup")]
        public static int DuplicateA;

        [Command("dup")]
        public static int DuplicateB;
    }
}
