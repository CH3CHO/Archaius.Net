using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class ConcurrentDictionaryConfigurationTest
    {
        private int m_EventCount;

        [Test]
        public void TestSetGet()
        {
            ConcurrentDictionaryConfiguration conf = new ConcurrentDictionaryConfiguration();
            conf.DelimiterParsingDisabled = false;
            conf.AddProperty("key1", "xyz");
            Assert.AreEqual("xyz", conf.GetProperty("key1"));
            conf.SetProperty("key1", "newProp");
            Assert.AreEqual("newProp", conf.GetProperty("key1"));
            conf.SetProperty("listProperty", "0,1,2,3");
            Assert.IsTrue(conf.GetProperty("listProperty") is IList);
            IList props = (IList)conf.GetProperty("listProperty");
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, int.Parse((string)props[i]));
            }
            conf.AddProperty("listProperty", "4");
            conf.AddProperty("listProperty", new List<string> {"5", "6"});
            props = (IList)conf.GetProperty("listProperty");
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(i, int.Parse((string)props[i]));
            }
            DateTime date = DateTime.Now;
            conf.SetProperty("listProperty", date);
            Assert.AreEqual(date, conf.GetProperty("listProperty"));
        }

        [Test]
        public void TestDelimiterParsingDisabled()
        {
            ConcurrentDictionaryConfiguration conf = new ConcurrentDictionaryConfiguration();
            conf.DelimiterParsingDisabled = true;
            conf.SetProperty("listProperty", "0,1,2,3");
            Assert.AreEqual("0,1,2,3", conf.GetProperty("listProperty"));
            conf.AddProperty("listProperty2", "0,1,2,3");
            Assert.AreEqual("0,1,2,3", conf.GetProperty("listProperty2"));
            conf.DelimiterParsingDisabled = false;
            Assert.AreEqual("0,1,2,3", conf.GetProperty("listProperty"));
            conf.SetProperty("anotherList", "0,1,2,3");
            var props = (IList)conf.GetProperty("anotherList");
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, int.Parse((string)props[i]));
            }
        }

        [Test]
        public void TestConcurrency()
        {
            ConcurrentDictionaryConfiguration conf = new ConcurrentDictionaryConfiguration();
            conf.DelimiterParsingDisabled = false;
            var threadCount = 20;
            var operationPerThread = 50;
            var expectedValueCount = threadCount * operationPerThread * 2;
            CountdownEvent doneEvent = new CountdownEvent(20);
            for (int i = 0; i < doneEvent.InitialCount; i++)
            {
                int index = i;
                new Thread(() =>
                           {
                               for (var j = 0; j < operationPerThread; ++j)
                               {
                                   conf.AddProperty("key", index);
                                   conf.AddProperty("key", "stringValue");
                               }
                               doneEvent.Signal();
                               Thread.Sleep(50);
                           }).Start();
            }
            doneEvent.Wait();
            IList prop = (IList)conf.GetProperty("key");
            Assert.AreEqual(expectedValueCount, prop.Count);
        }

        [Test]
        public void TestListeners()
        {
            ConcurrentDictionaryConfiguration conf = new ConcurrentDictionaryConfiguration();

            object eventSender = null;
            ConfigurationEventArgs eventArgs = null;
            conf.ConfigurationChanged += (sender, args) =>
                                       {
                                           eventSender = sender;
                                           eventArgs = args;
                                       };

            conf.AddProperty("key", "1");
            Assert.AreEqual(1, conf.GetInt("key"));
            Assert.AreEqual("key", eventArgs.Name);
            Assert.AreEqual("1", eventArgs.Value);
            Assert.AreSame(conf, eventSender);
            Assert.AreEqual(ConfigurationEventType.AddProperty, eventArgs.Type);
            conf.SetProperty("key", "2");

            Assert.AreEqual("key", eventArgs.Name);
            Assert.AreEqual("2", eventArgs.Value);
            Assert.AreSame(conf, eventSender);
            Assert.AreEqual(ConfigurationEventType.SetProperty, eventArgs.Type);

            conf.ClearProperty("key");
            Assert.AreEqual("key", eventArgs.Name);
            Assert.IsNull(eventArgs.Value);
            Assert.AreSame(conf, eventSender);
            Assert.AreEqual(ConfigurationEventType.ClearProperty, eventArgs.Type);

            conf.Clear();
            Assert.IsEmpty(conf.Keys);
            Assert.AreSame(conf, eventSender);
            Assert.AreEqual(ConfigurationEventType.Clear, eventArgs.Type);
        }

        [Test]
        public void TestPerformance()
        {
            var conf = new ConcurrentDictionaryConfiguration();
            conf.ConfigurationChanged += OnConfigurationChanged;
            TestConfigurationSet(conf);
            TestConfigurationAdd(conf);
            TestConfigurationGet(conf);
        }

        [Test]
        public void TestNullValue()
        {
            ConcurrentDictionaryConfiguration conf = new ConcurrentDictionaryConfiguration();
            try
            {
                conf.SetProperty("xyz", null);
                Assert.Fail("ArgumentNullException is expected.");
            }
            catch (ArgumentNullException e)
            {
            }
            try
            {
                conf.AddProperty("xyz", null);
                Assert.Fail("ArgumentNullException is expected.");
            }
            catch (ArgumentNullException e)
            {
            }
        }

        private void TestConfigurationSet(IConfiguration conf)
        {
            long start = Environment.TickCount;
            for (int i = 0; i < 1000000; i++)
            {
                conf.SetProperty("key" + +(i % 100), "value");
            }
            long duration = Environment.TickCount - start;
            Console.WriteLine("Set property for " + conf + " took " + duration + " ms");
        }

        private void TestConfigurationAdd(IConfiguration conf)
        {
            long start = Environment.TickCount;
            for (int i = 0; i < 100000; i++)
            {
                conf.AddProperty("add-key" + i, "value");
            }
            long duration = Environment.TickCount - start;
            Console.WriteLine("Add property for " + conf + " took " + duration + " ms");
        }

        private void TestConfigurationGet(IConfiguration conf)
        {
            long start = Environment.TickCount;
            for (int i = 0; i < 1000000; i++)
            {
                conf.GetProperty("key" + (i % 100));
            }
            long duration = Environment.TickCount - start;
            Console.WriteLine("get property for " + conf + " took " + duration + " ms");
        }

        private void OnConfigurationChanged(object sender, ConfigurationEventArgs args)
        {
            Interlocked.Increment(ref m_EventCount);
        }

        private void ResetEventCount()
        {
            m_EventCount = 0;
        }
    }
}