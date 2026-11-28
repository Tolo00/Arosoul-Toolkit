using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arosoul.Commands.Tests
{
    public class CommandParsingTests
    {
        [TestCase("SetDamage", "set-damage")]
        [TestCase("_maxSpeed", "max-speed")]
        [TestCase("m_Health", "health")]
        [TestCase("FPSCounter", "fps-counter")]
        [TestCase("damage", "damage")]
        [TestCase("Vector3Value", "vector3-value")]
        [TestCase("snake_case_name", "snake-case-name")]
        public void ToKebabCase(string memberName, string expected)
        {
            Assert.AreEqual(expected, CommandNaming.ToKebabCase(memberName));
        }

        [Test]
        public void Combine_JoinsPrefixWithDot()
        {
            Assert.AreEqual("player.weapon.damage", CommandNaming.Combine("player.weapon.", "damage"));
            Assert.AreEqual("damage", CommandNaming.Combine("", "damage"));
        }

        [TestCase("player.weapon.damage", true)]
        [TestCase("has space", false)]
        [TestCase("a=b", false)]
        [TestCase(".leading", false)]
        public void IsValidName(string name, bool expected)
        {
            Assert.AreEqual(expected, CommandNaming.IsValidName(name));
        }

        [Test]
        public void Tokenize_SplitsQuotesAndParentheses()
        {
            List<string> tokens = CommandTokenizer.Tokenize("spawn \"big goblin\" 3 (1, 2, 0) \"say \\\"hi\\\"\" \"\"");
            CollectionAssert.AreEqual(new[] { "spawn", "big goblin", "3", "(1, 2, 0)", "say \"hi\"", "" }, tokens);
        }

        [Test]
        public void TrySplitAssignment_RequiresSingleWordName()
        {
            Assert.IsTrue(CommandTokenizer.TrySplitAssignment("player.speed = (1, 2)", out string name, out string value));
            Assert.AreEqual("player.speed", name);
            Assert.AreEqual("(1, 2)", value);

            Assert.IsFalse(CommandTokenizer.TrySplitAssignment("echo a=b", out _, out _));
            Assert.IsFalse(CommandTokenizer.TrySplitAssignment("echo \"x = y\"", out _, out _));
        }

        [TestCase("time-scale = 2", "time-scale")]
        [TestCase("  spawn goblin", "spawn")]
        [TestCase("speed=3", "speed")]
        public void GetCommandName(string input, string expected)
        {
            Assert.AreEqual(expected, CommandTokenizer.GetCommandName(input));
        }

        [Test]
        public void Parse_Primitives()
        {
            Assert.AreEqual(42, Parse<int>("42"));
            Assert.AreEqual(-3L, Parse<long>("-3"));
            Assert.AreEqual(1.5f, Parse<float>("1.5"));
            Assert.AreEqual(0.25f, Parse<float>("0.25f"));
            Assert.AreEqual(2.5, Parse<double>("2.5"));
            Assert.AreEqual(true, Parse<bool>("on"));
            Assert.AreEqual(false, Parse<bool>("No"));
            Assert.AreEqual('x', Parse<char>("x"));
            Assert.AreEqual("hello world", Parse<string>("\"hello world\""));
            Assert.IsFalse(ValueParsers.TryParse("abc", typeof(int), out _));
            Assert.IsFalse(ValueParsers.TryParse("1,5", typeof(float), out _));
        }

        [Test]
        public void Parse_Enums()
        {
            Assert.AreEqual(TestMode.GodMode, Parse<TestMode>("GodMode"));
            Assert.AreEqual(TestMode.GodMode, Parse<TestMode>("god-mode"));
            Assert.AreEqual(TestMode.NoClip, Parse<TestMode>("2"));
            Assert.AreEqual(TestFlags.A | TestFlags.C, Parse<TestFlags>("A|c"));
            Assert.IsFalse(ValueParsers.TryParse("Flying", typeof(TestMode), out _));
        }

        [Test]
        public void Parse_Vectors()
        {
            Assert.AreEqual(new Vector2(1f, 2f), Parse<Vector2>("(1, 2)"));
            Assert.AreEqual(new Vector3(1f, 2.5f, -3f), Parse<Vector3>("1,2.5,-3"));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), Parse<Vector3>("1 2 3"));
            Assert.AreEqual(new Vector4(1f, 2f, 3f, 4f), Parse<Vector4>("[1, 2, 3, 4]"));
            Assert.AreEqual(new Vector3Int(1, 2, 3), Parse<Vector3Int>("(1, 2, 3)"));
            Assert.AreEqual(new Vector2Int(4, 5), Parse<Vector2Int>("(4, 5)"));
            Assert.IsFalse(ValueParsers.TryParse("(1, 2)", typeof(Vector3), out _));

            Quaternion rotation = Parse<Quaternion>("(0, 90, 0)");
            Assert.That(Quaternion.Angle(rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void Parse_Colors()
        {
            Assert.AreEqual(Color.red, Parse<Color>("red"));
            Assert.AreEqual(Color.red, Parse<Color>("Red"));
            Assert.AreEqual(new Color(1f, 0f, 0f, 1f), Parse<Color>("#FF0000"));
            Assert.AreEqual(new Color(0f, 1f, 0f, 1f), Parse<Color>("00FF00"));
            Assert.AreEqual(new Color32(255, 0, 0, 128), Parse<Color32>("#FF000080"));
            Assert.IsFalse(ValueParsers.TryParse("notacolor", typeof(Color), out _));
        }

        [Test]
        public void CustomParser_IsUsedOnceRegistered()
        {
            Assert.IsFalse(ValueParsers.CanParse(typeof(CustomValue)));

            ValueParsers.Register(new CustomValueParser());
            try
            {
                Assert.IsTrue(ValueParsers.CanParse(typeof(CustomValue)));
                Assert.AreEqual("abc", Parse<CustomValue>("abc").Text);
            }
            finally
            {
                ValueParsers.Unregister(typeof(CustomValue));
            }
        }

        [Test]
        public void Format_UsesParsableSyntax()
        {
            Assert.AreEqual("(1, 2.5, 0)", ValueFormatter.Format(new Vector3(1f, 2.5f, 0f)));
            Assert.AreEqual("#FF0000FF", ValueFormatter.Format(Color.red));
            Assert.AreEqual("1.5", ValueFormatter.Format(1.5f));
            Assert.AreEqual("true", ValueFormatter.Format(true));
            Assert.AreEqual("[1, 2, 3]", ValueFormatter.Format(new[] { 1, 2, 3 }));
            Assert.AreEqual("null", ValueFormatter.Format(null));
        }

        [Test]
        public void FriendlyTypeNames()
        {
            Assert.AreEqual("int", TypeNames.GetFriendlyName(typeof(int)));
            Assert.AreEqual("List<string>", TypeNames.GetFriendlyName(typeof(List<string>)));
            Assert.AreEqual("Vector3[]", TypeNames.GetFriendlyName(typeof(Vector3[])));
        }

        private static T Parse<T>(string text)
        {
            Assert.IsTrue(ValueParsers.TryParse(text, typeof(T), out object value), $"Could not parse '{text}' as {typeof(T).Name}.");
            return (T)value;
        }

        private class CustomValue
        {
            public string Text;
        }

        private class CustomValueParser : ValueParser<CustomValue>
        {
            public override bool TryParse(string text, out CustomValue value)
            {
                value = new CustomValue { Text = text };
                return true;
            }
        }
    }
}
