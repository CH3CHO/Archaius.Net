using System;
using System.IO;
using Archaius.Dynamic;
using Archaius.Source;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class UrlConfigurationSourceTest
    {
        private const string PropertyName = "biz.mindyourown.notMine";
        private const string PropertyName2 = "biz.mindyourown.myProperty";

        private static string m_ConfigFile1;
        private static string m_ConfigFile2;
        private static UrlConfigurationSource m_Source;

        private static void CreateConfigFile()
        {
            m_ConfigFile1 = Path.GetTempFileName();
            using (var writer = new StreamWriter(m_ConfigFile1, false))
            {
                writer.WriteLine("prop1=xyz");
                writer.WriteLine("prop2=abc");
            }
            m_ConfigFile2 = Path.GetTempFileName();
            using (var writer = new StreamWriter(m_ConfigFile2, false))
            {
                writer.WriteLine("prop2=def");
                writer.WriteLine("prop3=123");
            }
        }

        [TestFixtureSetUp]
        public static void FixtureSetUp()
        {
            CreateConfigFile();

            m_Source = new UrlConfigurationSource(m_ConfigFile1, m_ConfigFile2);
            Console.WriteLine("Initializing with sources: " + string.Join(m_ConfigFile1, m_ConfigFile2));
        }

        [TestFixtureTearDown]
        public static void FixtureTearDown()
        {
            if (File.Exists(m_ConfigFile1))
            {
                File.Delete(m_ConfigFile1);
            }
            if (File.Exists(m_ConfigFile2))
            {
                File.Delete(m_ConfigFile2);
            }
        }

        [Test]
        public void PropertyOverwrittenTest()
        {
            var checkPoint = new object();
            var result = m_Source.Poll(true, checkPoint);
            Assert.IsNull(result.CheckPoint);
            Assert.IsTrue(result.HasChanges);
            Assert.IsFalse(result.Incremental);
            Assert.IsNull(result.Added);
            Assert.IsNull(result.Changed);
            Assert.IsNull(result.Deleted);
            Assert.AreEqual(3, result.Complete.Count);
            Assert.AreEqual("xyz", result.Complete["prop1"]);
            Assert.AreEqual("def", result.Complete["prop2"]);
            Assert.AreEqual("123", result.Complete["prop3"]);
        }
    }
}