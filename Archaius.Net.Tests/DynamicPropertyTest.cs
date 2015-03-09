using System;
using System.IO;
using System.Linq;
using System.Threading;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class DynamicPropertyTest
    {
        private const string PropertyName = "biz.mindyourown.notMine";
        private const string PropertyName2 = "biz.mindyourown.myProperty";

        private static string m_ConfigFile;
        private static DynamicConfiguration m_Config;

        private static void CreateConfigFile()
        {
            m_ConfigFile = Path.GetTempFileName();
            using (var writer = new StreamWriter(m_ConfigFile, false))
            {
                writer.WriteLine("props1=xyz");
                writer.WriteLine("props2=abc");
            }
        }

        private static void ModifyConfigFile()
        {
            using (var writer = new StreamWriter(m_ConfigFile, false))
            {
                writer.WriteLine("props2=456");
                writer.WriteLine("props3=123");
            }
        }

        [TestFixtureSetUp]
        public static void FixtureSetUp()
        {
            CreateConfigFile();

            m_Config = new DynamicUrlConfiguration(100, 500, false, m_ConfigFile);
            Console.WriteLine("Initializing with sources: " + m_Config.Source);
            DynamicPropertyFactory.InitWithConfigurationSource(m_Config);
        }

        [TestFixtureTearDown]
        public static void FixtureTearDown()
        {
            if (File.Exists(m_ConfigFile))
            {
                File.Delete(m_ConfigFile);
            }
        }

        [Test]
        public void TestAsFileBased()
        {
            DynamicStringProperty prop = new DynamicStringProperty("props1", null);
            DynamicStringProperty prop2 = new DynamicStringProperty("props2", null);
            DynamicIntProperty prop3 = new DynamicIntProperty("props3", 0);
            Thread.Sleep(1000);
            Assert.AreEqual("xyz", prop.Value);
            Assert.AreEqual("abc", prop2.Value);
            Assert.AreEqual(0, prop3.Value);
            ModifyConfigFile();
            // Waiting for reload
            Thread.Sleep(2000);
            Assert.IsNull(prop.Value);
            Assert.AreEqual("456", prop2.Value);
            Assert.AreEqual(123, prop3.Value);
            m_Config.StopLoading();
            Thread.Sleep(2000);
            m_Config.SetProperty("props2", "000");
            Assert.AreEqual("000", prop2.Value);
        }

        [Test]
        public void TestDynamicProperty()
        {
            m_Config.StopLoading();
            DynamicProperty fastProp = DynamicProperty.GetInstance(PropertyName);
            Assert.AreEqual(PropertyName, fastProp.Name, "FastProperty does not have correct name");
            Assert.AreSame(fastProp, DynamicProperty.GetInstance(PropertyName), "DynamicProperty.GetInstance did not find the object");

            var hello = "Hello";
            Assert.IsNull(fastProp.GetString(), "Unset DynamicProperty is not null");
            Assert.AreEqual(hello, fastProp.GetString(hello), "Unset DynamicProperty does not default correctly");
            m_Config.SetProperty(PropertyName, hello);
            Assert.AreEqual(hello, fastProp.GetString(), "Set DynamicProperty does not have correct value");
            Assert.AreEqual(hello, fastProp.GetString("not " + hello), "Set DynamicProperty uses supplied default");
            Assert.AreEqual(123, fastProp.GetInteger(123), "Non-integer DynamicProperty doesn't default on integer fetch");
            Assert.AreEqual(2.71838f, fastProp.GetFloat(2.71838f), 0.001f, "Non-float DynamicProperty doesn't default on float fetch");
            try
            {
                fastProp.GetFloat();
                Assert.Fail("Parse should have failed:  " + fastProp);
            }
            catch (ArgumentException e)
            {
            }

            var pi = "3.14159";
            var ee = "2.71838";
            m_Config.SetProperty(PropertyName, pi);
            Assert.AreEqual(pi, fastProp.GetString(), "Set DynamicProperty does not have correct value");
            Assert.AreEqual(3.14159f, fastProp.GetFloat(0.0f), 0.001f, "DynamicProperty did not property parse float string");
            m_Config.SetProperty(PropertyName, ee);
            Assert.AreEqual(ee, fastProp.GetString(), "Set DynamicProperty does not have correct value");
            Assert.AreEqual(2.71838f, fastProp.GetFloat(0.0f), 0.001f, "DynamicProperty did not property parse float string");
            try
            {
                fastProp.GetInteger();
                Assert.Fail("Integer fetch of non-integer DynamicProperty should have failed:  " + fastProp);
            }
            catch (ArgumentException e)
            {
            }
            Assert.AreEqual(-123, fastProp.GetInteger(-123), "Integer fetch of non-integer DynamicProperty did not use default value");

            var devil = "666";
            m_Config.SetProperty(PropertyName, devil);
            Assert.AreEqual(devil, fastProp.GetString(), "Changing DynamicProperty does not result in correct value");
            Assert.AreEqual(666, fastProp.GetInteger(), "Integer fetch of changed DynamicProperty did not return correct value");

            var self = "Archaius.Dynamic.DynamicProperty";
            Assert.AreEqual(typeof(DynamicPropertyTest), fastProp.GetNamedType(typeof(DynamicPropertyTest)),
                            "Fetch of named class from integer valued DynamicProperty did not use default");
            m_Config.SetProperty(PropertyName, self);
            Assert.AreEqual(typeof(DynamicProperty), fastProp.GetNamedType(), "Fetch of named class from DynamicProperty did not find the class");

            // Check that clearing a property clears all caches
            m_Config.ClearProperty(PropertyName);
            Assert.IsNull(fastProp.GetString(), "Fetch of cleard property did not return null");
            Assert.AreEqual(devil, fastProp.GetString(devil), "Fetch of cleard property did not use default value");
            Assert.AreEqual(0, fastProp.GetInteger(), "Fetch of cleard property did not return default value");
            Assert.AreEqual(-123, fastProp.GetInteger(-123), "Fetch of cleard property did not use default value");
            Assert.AreEqual(0.0f, fastProp.GetFloat(), "Fetch of cleard property did not return null");
            Assert.AreEqual(2.71838f, fastProp.GetFloat(2.71838f), 0.001f, "Fetch of cleard property did not use default value");
            Assert.IsNull(fastProp.GetNamedType(), "Fetch of cleard property did not return null");
            Assert.AreEqual(typeof(DynamicProperty), fastProp.GetNamedType(typeof(DynamicProperty)),
                            "Fetch of cleard property did not use default value");
            //
            var yes = "yes";
            var maybe = "maybe";
            var no = "Off";
            m_Config.SetProperty(PropertyName, yes);
            Assert.IsTrue(fastProp.GetBoolean(), "boolean property set to 'yes' is not true");
            m_Config.SetProperty(PropertyName, no);
            Assert.IsFalse(fastProp.GetBoolean(), "boolean property set to 'no' is not false");
            m_Config.SetProperty(PropertyName, maybe);
            try
            {
                fastProp.GetBoolean();
                Assert.Fail("Parse should have failed: " + fastProp);
            }
            catch (ArgumentException e)
            {
            }
            Assert.IsTrue(fastProp.GetBoolean(true));
            Assert.IsFalse(fastProp.GetBoolean(false));
        }

        [Test]
        public void TestPerformance()
        {
            m_Config.StopLoading();
            DynamicProperty fastProp = DynamicProperty.GetInstance(PropertyName2);
            String goodbye = "Goodbye";
            int loopCount = 1000000;
            m_Config.SetProperty(PropertyName2, goodbye);
            long cnt = 0;
            long start = Environment.TickCount;
            for (int i = 0; i < loopCount; i++)
            {
                cnt += fastProp.GetString().Length;
            }
            long elapsed = Environment.TickCount - start;
            Console.WriteLine("Fetched dynamic property " + loopCount + " times in " + elapsed + " milliseconds");
            // Now for the "normal" time
            cnt = 0;
            start = Environment.TickCount;
            for (int i = 0; i < loopCount; i++)
            {
                cnt += m_Config.GetString(PropertyName2).Length;
            }
            elapsed = Environment.TickCount - start;
            Console.WriteLine("Fetched Configuration value " + loopCount + " times in " + elapsed + " milliseconds");
            // Now for the "system property" time
            cnt = 0;
            Environment.SetEnvironmentVariable(PropertyName2, goodbye, EnvironmentVariableTarget.Process);
            start = Environment.TickCount;
            for (int i = 0; i < loopCount; i++)
            {
                cnt += Environment.GetEnvironmentVariable(PropertyName2).Length;
            }
            elapsed = Environment.TickCount - start;
            Console.WriteLine("Fetched system property value " + loopCount + " times in " + elapsed + " milliseconds");
        }

        [Test]
        public void TestDynamicPropertyListenerPropertyChangeCallback()
        {
            m_Config.StopLoading();
            var testProperty = new TestDynamicStringProperty("Archaius.Net.TestCallback", "");
            m_Config.SetProperty(testProperty.Name, "valuechanged");
            Assert.IsTrue(testProperty.OnPropertyChangedCalled, "propertyChanged did not get called");
            Assert.AreEqual("valuechanged", testProperty.Value);
        }

        private class TestDynamicStringProperty : DynamicStringProperty
        {
            public TestDynamicStringProperty(string propName, string defaultValue) : base(propName, defaultValue)
            {
            }

            public bool OnPropertyChangedCalled
            {
                get;
                private set;
            }

            protected override void OnPropertyChanged()
            {
                OnPropertyChangedCalled = true;
            }
        }

        [Test]
        public void TestFastProperyTimestamp()
        {
            m_Config.StopLoading();
            DynamicStringProperty prop = new DynamicStringProperty("Archaius.Net.Test.Timestamp", "hello");
            DateTime initialTime = prop.ChangedTime;
            Thread.Sleep(10);
            Assert.AreEqual(initialTime, prop.ChangedTime);
            m_Config.SetProperty(prop.Name, "goodbye");
            Assert.IsTrue((prop.ChangedTime - initialTime) > TimeSpan.FromMilliseconds(8));
        }

        [Test]
        public void TestDynamicProperySetAdnGets()
        {
            m_Config.StopLoading();
            var prop = new DynamicBooleanProperty("Archaius.Net.Test.MyBool", false);
            Assert.IsFalse(prop.Value);
            Assert.AreEqual(0, prop.Property.PropertyChangedHandlers.Length);
            for (var i = 0; i < 10; i++)
            {
                m_Config.SetProperty(prop.Name, "true");
                Assert.IsTrue(prop.Value);
                Assert.AreEqual("true", m_Config.GetString(prop.Name));
                m_Config.SetProperty(prop.Name, "false");
                Assert.IsFalse(prop.Value);
                Assert.AreEqual("false", m_Config.GetString(prop.Name));
            }
            for (int i = 0; i < 100; i++)
            {
                m_Config.SetProperty(prop.Name, "true");
                Assert.IsTrue(prop.Value);
                Assert.AreEqual("true", m_Config.GetString(prop.Name));
                m_Config.ClearProperty(prop.Name);
                Assert.IsFalse(prop.Value);
                Assert.IsNull(m_Config.GetString(prop.Name));
            }
        }

        [Test]
        public void TestPropertyCreation()
        {
            m_Config.StopLoading();
            const string newValue = "newValue";
            var callbackTriggered = false;
            EventHandler callback = (s, a) => callbackTriggered = true;
            var prop = DynamicPropertyFactory.GetInstance().GetStringProperty("foo.bar", "xyz", callback);
            Assert.AreEqual("xyz", prop.Value);
            m_Config.SetProperty("foo.bar", newValue);
            Assert.IsTrue(callbackTriggered);
            Assert.AreEqual(newValue, prop.Value);
            Assert.IsTrue(prop.Property.PropertyChangedHandlers.Any(h => h == callback));
        }
    }
}