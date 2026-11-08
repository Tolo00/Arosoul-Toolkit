using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arosoul.Essentials.Tests {

    public struct TestData : ISaveableData {
        public float testFloat;
        public int testInt;
        public string testString;
    }

    public class DataLoaderTests {
        
        
        [Test]
        public void SaveLoadDataCompact() {
            DataSaver<TestData> dataSaver = new DataSaver<TestData>("TestData", SaveFormat.Compact);
            TestData testData = new TestData() {
                testFloat = 23.54f,
                testInt = 35581,
                testString = "This is a test string!"
            };

            dataSaver.Save(testData);
            dataSaver.Load(out TestData loadedTestData);

            Assert.AreEqual(loadedTestData, testData);
        }

        [Test]
        public void SaveLoadDataEncrypted() {
            DataSaver<TestData> dataSaver = new DataSaver<TestData>("TestData", SaveFormat.Encrypted);
            TestData testData = new TestData() {
                testFloat = 23.54f,
                testInt = 35581,
                testString = "This is a test string!"
            };

            dataSaver.Save(testData);
            dataSaver.Load(out TestData loadedTestData);

            Assert.AreEqual(loadedTestData, testData);
        }
    }

}
