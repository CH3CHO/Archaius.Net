using System.Collections;
using System.Collections.Generic;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class DynamicPropertyUpdaterTest
    {
        private DynamicPropertyUpdater m_DynamicPropertyUpdater;
        private int m_EventCount;

        [SetUp]
        public void SetUp()
        {
            m_DynamicPropertyUpdater = new DynamicPropertyUpdater();
        }

        [Test]
        public void TestUpdateProperties()
        {
            AbstractConfiguration.DefaultListDelimiter = ',';
            AbstractConfiguration config = new ConcurrentCompositeConfiguration();
            config.ConfigurationChanged += OnConfigurationChanged;
            ResetEventCount();
            config.SetProperty("test", "host,host1,host2");
            config.SetProperty("test12", "host12");
            var added = new Dictionary<string, object>();
            added.Add("test.host", "test,test1");
            var changed = new Dictionary<string, object>();
            changed.Add("test", "host,host1");
            changed.Add("test.host", "");
            m_DynamicPropertyUpdater.UpdateProperties(WatchedUpdateResult.CreateIncremental(added, changed, null), config, false);
            Assert.AreEqual("", config.GetProperty("test.host"));
            Assert.AreEqual(2, ((IList)(config.GetProperty("test"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test"))).Contains("host"));
            Assert.IsTrue(((IList)(config.GetProperty("test"))).Contains("host1"));
            Assert.AreEqual(5, m_EventCount);
        }

        [Test]
        public void TestAddOrChangeProperty()
        {
            AbstractConfiguration.DefaultListDelimiter = ',';
            AbstractConfiguration config = new ConcurrentCompositeConfiguration();
            config.ConfigurationChanged += OnConfigurationChanged;
            ResetEventCount();
            config.SetProperty("test.host", "test,test1,test2");
            Assert.AreEqual(1, m_EventCount);
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host", "test,test1,test2", config);
            Assert.AreEqual(3, ((IList)(config.GetProperty("test.host"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test1"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test2"));
            Assert.AreEqual(1, m_EventCount);
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host", "test,test1,test2", config);
            Assert.AreEqual(3, ((IList)(config.GetProperty("test.host"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test1"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test2"));
            Assert.AreEqual(1, m_EventCount);
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host", "test,test1", config);
            Assert.AreEqual(2, ((IList)(config.GetProperty("test.host"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test1"));
            Assert.AreEqual(2, m_EventCount);

            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host1", "test1,test12", config);
            Assert.AreEqual(2, ((IList)(config.GetProperty("test.host1"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test.host1"))).Contains("test1"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host1"))).Contains("test12"));
            Assert.AreEqual(3, m_EventCount);

            config.SetProperty("test.host1", "test1.test12");
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host1", "test1.test12", config);
            Assert.AreEqual("test1.test12", config.GetProperty("test.host1"));
            Assert.AreEqual(4, m_EventCount);
        }

        [Test]
        public void TestAddorUpdatePropertyWithColonDelimiter()
        {
            AbstractConfiguration.DefaultListDelimiter = ':';
            AbstractConfiguration config = new ConcurrentCompositeConfiguration();
            config.ConfigurationChanged += OnConfigurationChanged;
            ResetEventCount();
            config.SetProperty("test.host", "test:test1:test2");
            Assert.AreEqual(1, m_EventCount);
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host", "test:test1:test2", config);
            Assert.AreEqual(3, ((IList)(config.GetProperty("test.host"))).Count);
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test1"));
            Assert.IsTrue(((IList)(config.GetProperty("test.host"))).Contains("test2"));
            Assert.AreEqual(1, m_EventCount); // the config is not set again. when the value is still not changed.
            config.SetProperty("test.host1", "test1:test12");
            // Changing the new object value, the config.SetProperty should be called again.
            m_DynamicPropertyUpdater.AddOrChangeProperty("test.host1", "test1.test12", config);
            Assert.AreEqual("test1.test12", config.GetProperty("test.host1"));
            Assert.AreEqual(3, m_EventCount);
        }

        private void OnConfigurationChanged(object sender, ConfigurationEventArgs args)
        {
            if (args.BeforeOperation)
            {
                return;
            }
            switch (args.Type)
            {
                case ConfigurationEventType.AddProperty:
                case ConfigurationEventType.SetProperty:
                case ConfigurationEventType.ClearProperty:
                    ++m_EventCount;
                    break;
            }
        }

        private void ResetEventCount()
        {
            m_EventCount = 0;
        }
    }
}