using System;
using System.Collections.Generic;
using System.Linq;
using Arosoul.PlayerSettings.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Arosoul.PlayerSettings.Tests {

    public class CustomSettingsValidationTests {

        static string[] Validate(params CustomSettingData[] rows) => PlayerSettingsAgentTools.ValidateCustomSettings(rows);

        [Test]
        public void ValidRows_HaveNoErrors() {
            CollectionAssert.IsEmpty(Validate(
                new CustomSettingData { apiName = "Sensitivity", type = SettingType.Float, min = 0.1f, max = 2f, defaultValue = "0.7" },
                new CustomSettingData { apiName = "AimAssist", type = SettingType.Enum, options = new[] { "Off", "Low", "High" }, defaultValue = "Low" },
                new CustomSettingData { apiName = "ShowHints", type = SettingType.Bool, defaultValue = "true" }));
        }

        [TestCase("2Fast")]
        [TestCase("class")]
        [TestCase("Has Space")]
        [TestCase("Data")]
        [TestCase("vsync")]
        public void InvalidApiNames_AreRejected(string apiName) {
            CollectionAssert.IsNotEmpty(Validate(new CustomSettingData { apiName = apiName, type = SettingType.Bool, defaultValue = "true" }));
        }

        [Test]
        public void DuplicateApiNames_AreRejected() {
            var row = new CustomSettingData { apiName = "Same", type = SettingType.Bool, defaultValue = "true" };
            CollectionAssert.IsNotEmpty(Validate(row, row.Clone()));
        }

        [Test]
        public void NumericRanges_AreChecked() {
            CollectionAssert.IsNotEmpty(Validate(new CustomSettingData { apiName = "A", type = SettingType.Int, min = 5, max = 5, defaultValue = "5" }));
            CollectionAssert.IsNotEmpty(Validate(new CustomSettingData { apiName = "B", type = SettingType.Float, min = 0, max = 1, defaultValue = "2" }));
        }

        [Test]
        public void EnumDefault_MustBeAnOption() {
            CollectionAssert.IsNotEmpty(Validate(new CustomSettingData { apiName = "Mode", type = SettingType.Enum, options = new[] { "A", "B" }, defaultValue = "C" }));
        }
    }

    public class LayoutAuthoringTests {
        SettingsLayout layout;

        [SetUp]
        public void SetUp() => layout = ScriptableObject.CreateInstance<SettingsLayout>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(layout);

        [Test]
        public void AddEntry_PicksDefaultDisplay_AndValidatesExplicitOnes() {
            PlayerSettingsAgentTools.AddCategory(layout, "Video");
            int video = PlayerSettingsAgentTools.FindCategoryIndex(layout, "Video");
            Assert.AreEqual(0, video);
            Assert.AreEqual(-1, PlayerSettingsAgentTools.FindCategoryIndex(layout, "Missing"));

            var vsync = PlayerSettingsAgentTools.AddEntry(layout, video, "vsync");
            Assert.AreEqual("toggle", vsync.displayType);

            var volume = PlayerSettingsAgentTools.AddEntry(layout, video, "master_volume", "stepper");
            Assert.AreEqual("stepper", volume.displayType);

            Assert.Throws<ArgumentException>(() => PlayerSettingsAgentTools.AddEntry(layout, video, "vsync", "slider"));
            Assert.Throws<ArgumentException>(() => PlayerSettingsAgentTools.AddEntry(layout, video, "does_not_exist"));
            CollectionAssert.IsEmpty(PlayerSettingsAgentTools.ValidateLayout(layout));
        }

        [Test]
        public void Decorations_ValidateTheirParameters() {
            PlayerSettingsAgentTools.AddCategory(layout, "General");
            PlayerSettingsAgentTools.AddEntry(layout, 0, "decoration:spacing", null, new Dictionary<string, string> { { "height", "12.5" } });

            Assert.Throws<ArgumentException>(() => PlayerSettingsAgentTools.AddEntry(layout, 0, "decoration:spacing", null, new Dictionary<string, string> { { "height", "tall" } }));
            Assert.Throws<ArgumentException>(() => PlayerSettingsAgentTools.AddEntry(layout, 0, "decoration:spacing", null, new Dictionary<string, string> { { "width", "1" } }));
            CollectionAssert.IsEmpty(PlayerSettingsAgentTools.ValidateLayout(layout));
        }

        [Test]
        public void ValidateLayout_ReportsUnknownIdsAndIncompatibleDisplays() {
            PlayerSettingsAgentTools.AddCategory(layout, "General");
            layout.Categories[0].entries.Add(new EntryLayout { settingId = "does_not_exist" });
            layout.Categories[0].entries.Add(new EntryLayout { settingId = "vsync", displayType = "slider" });

            string[] diagnostics = PlayerSettingsAgentTools.ValidateLayout(layout);
            Assert.IsTrue(diagnostics.Any(d => d.StartsWith("ERROR:") && d.Contains("does_not_exist")));
            Assert.IsTrue(diagnostics.Any(d => d.StartsWith("WARNING:") && d.Contains("vsync")));
        }

        [Test]
        public void MoveAndRemove_UseCurrentIndices() {
            PlayerSettingsAgentTools.AddCategory(layout, "A");
            PlayerSettingsAgentTools.AddCategory(layout, "B");
            PlayerSettingsAgentTools.MoveCategory(layout, 1, 0);
            Assert.AreEqual("B", layout.Categories[0].name);

            Assert.IsTrue(PlayerSettingsAgentTools.RemoveCategory(layout, 0));
            Assert.AreEqual("A", layout.Categories[0].name);
            StringAssert.Contains("[0] 'A'", PlayerSettingsAgentTools.DescribeLayout(layout));
        }
    }

}
