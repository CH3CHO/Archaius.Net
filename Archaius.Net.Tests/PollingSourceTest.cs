using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Archaius.Dynamic;
using Archaius.Source;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class PollingSourceTest
    {
        [Test]
        public void TestDeletingPollingSource()
        {
            ConcurrentDictionaryConfiguration config = new ConcurrentDictionaryConfiguration();
            config.AddProperty("prop1", "original");
            DummyPollingSource source = new DummyPollingSource(false);
            source.SetFull("prop1=changed");
            FixedDelayPollingScheduler scheduler = new FixedDelayPollingScheduler(0, 10, false);
            ConfigurationWithPollingSource pollingConfig = new ConfigurationWithPollingSource(config, source, scheduler);
            Thread.Sleep(200);
            Assert.AreEqual("changed", pollingConfig.GetProperty("prop1"));

            source.SetFull("");
            Thread.Sleep(250);
            Assert.IsFalse(pollingConfig.ContainsKey("prop1"));
            source.SetFull("prop1=changedagain,prop2=new");
            Thread.Sleep(200);
            Assert.AreEqual("changedagain", pollingConfig.GetProperty("prop1"));
            Assert.AreEqual("new", pollingConfig.GetProperty("prop2"));
            source.SetFull("prop3=new");
            Thread.Sleep(200);
            Assert.IsFalse(pollingConfig.ContainsKey("prop1"));
            Assert.IsFalse(pollingConfig.ContainsKey("prop2"));
            Assert.AreEqual("new", pollingConfig.GetProperty("prop3"));
        }

        [Test]
        public void TestNoneDeletingPollingSource()
        {
            var config = new ConcurrentDictionaryConfiguration();
            config.AddProperty("prop1", "original");
            DummyPollingSource source = new DummyPollingSource(false);
            source.SetFull("");
            FixedDelayPollingScheduler scheduler = new FixedDelayPollingScheduler(0, 10, true);
            ConfigurationWithPollingSource pollingConfig = new ConfigurationWithPollingSource(config, source, scheduler);
            Thread.Sleep(200);
            Assert.AreEqual("original", pollingConfig.GetProperty("prop1"));
            source.SetFull("prop1=changed");
            Thread.Sleep(200);
            Assert.AreEqual("changed", pollingConfig.GetProperty("prop1"));
            source.SetFull("prop1=changedagain,prop2=new");
            Thread.Sleep(200);
            Assert.AreEqual("changedagain", pollingConfig.GetProperty("prop1"));
            Assert.AreEqual("new", pollingConfig.GetProperty("prop2"));
            source.SetFull("prop3=new");
            Thread.Sleep(200);
            Assert.AreEqual("changedagain", pollingConfig.GetProperty("prop1"));
            Assert.AreEqual("new", pollingConfig.GetProperty("prop2"));
            Assert.AreEqual("new", pollingConfig.GetProperty("prop3"));
        }

        [Test]
        public void TestIncrementalPollingSource()
        {
            var config = new ConcurrentDictionaryConfiguration();
            DynamicPropertyFactory.InitWithConfigurationSource(config);
            DynamicStringProperty prop1 = new DynamicStringProperty("prop1", null);
            DynamicStringProperty prop2 = new DynamicStringProperty("prop2", null);
            config.AddProperty("prop1", "original");
            DummyPollingSource source = new DummyPollingSource(true);
            FixedDelayPollingScheduler scheduler = new FixedDelayPollingScheduler(0, 10, true);
            scheduler.IgnoreDeletesFromSource = false;
            // ConfigurationWithPollingSource pollingConfig = new ConfigurationWithPollingSource(config, source,scheduler);
            scheduler.StartPolling(source, config);
            Assert.AreEqual("original", config.GetProperty("prop1"));
            Assert.AreEqual("original", prop1.Value);
            source.SetAdded("prop2=new");
            Thread.Sleep(200);
            Assert.AreEqual("original", config.GetProperty("prop1"));
            Assert.AreEqual("new", config.GetProperty("prop2"));
            Assert.AreEqual("new", prop2.Value);
            source.SetDeleted("prop1=DoesNotMatter");
            source.SetChanged("prop2=changed");
            source.SetAdded("");
            Thread.Sleep(200);
            Assert.IsFalse(config.ContainsKey("prop1"));
            Assert.IsNull(prop1.Value);
            Assert.AreEqual("changed", config.GetProperty("prop2"));
            Assert.AreEqual("changed", prop2.Value);
        }

        private class DummyPollingSource : IPolledConfigurationSource
        {
            private volatile bool m_Incremental;
            private volatile IDictionary<string, object> m_Full, m_Added, m_Deleted, m_Changed;
            private readonly object m_ObjectLock = new object();

            public DummyPollingSource(bool incremental)
            {
                m_Incremental = incremental;
            }

            public void SetIncremental(bool value)
            {
                lock (m_ObjectLock)
                {
                    m_Incremental = value;
                }
            }

            private void SetContent(string content, IDictionary<string, object> dict)
            {
                lock (m_ObjectLock)
                {
                    var pairs = content.Split(',');
                    foreach (var pair in pairs)
                    {
                        var nameValue = pair.Trim().Split('=');
                        if (nameValue.Length == 2)
                        {
                            dict.Add(nameValue[0], nameValue[1]);
                        }
                    }
                }
            }

            public void SetFull(string content)
            {
                lock (m_ObjectLock)
                {
                    m_Full = new ConcurrentDictionary<string, object>();
                    SetContent(content, m_Full);
                }
            }

            public void SetAdded(String content)
            {
                lock (m_ObjectLock)
                {
                    m_Added = new ConcurrentDictionary<String, Object>();
                    SetContent(content, m_Added);
                }
            }

            public void SetDeleted(String content)
            {
                m_Deleted = new ConcurrentDictionary<String, Object>();
                SetContent(content, m_Deleted);
            }

            public void SetChanged(String content)
            {
                m_Changed = new ConcurrentDictionary<String, Object>();
                SetContent(content, m_Changed);
            }

            public PollResult Poll(bool initial, object checkPoint)
            {
                if (m_Incremental)
                {
                    return PollResult.CreateIncremental(m_Added, m_Changed, m_Deleted, checkPoint);
                }
                return PollResult.CreateFull(m_Full);
            }
        }
    }
}