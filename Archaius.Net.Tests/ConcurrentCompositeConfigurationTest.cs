using System;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class ConcurrentCompositeConfigurationTest
    {
        [Test]
        public void TestProperties()
        {
            ConcurrentCompositeConfiguration config = new ConcurrentCompositeConfiguration();
            DynamicPropertyFactory factory = DynamicPropertyFactory.InitWithConfigurationSource(config);
            DynamicStringProperty prop1 = factory.GetStringProperty("prop1", null);
            DynamicStringProperty prop2 = factory.GetStringProperty("prop2", null);
            DynamicStringProperty prop3 = factory.GetStringProperty("prop3", null);
            DynamicStringProperty prop4 = factory.GetStringProperty("prop4", null);
            AbstractConfiguration containerConfig = new ConcurrentDictionaryConfiguration();
            containerConfig.AddProperty("prop1", "prop1");
            containerConfig.AddProperty("prop2", "prop2");
            AbstractConfiguration baseConfig = new ConcurrentDictionaryConfiguration();
            baseConfig.AddProperty("prop3", "prop3");
            baseConfig.AddProperty("prop1", "prop1FromBase");
            // Make container configuration the highest priority
            config.SetContainerConfiguration(containerConfig, "container configuration", 0);
            config.AddConfiguration(baseConfig, "base configuration");
            Assert.AreEqual("prop1", config.GetProperty("prop1"));
            Assert.AreEqual("prop1", prop1.Value);
            Assert.AreEqual("prop2", prop2.Value);
            Assert.AreEqual("prop3", prop3.Value);
            containerConfig.SetProperty("prop1", "newvalue");
            Assert.AreEqual("newvalue", prop1.Value);
            Assert.AreEqual("newvalue", config.GetProperty("prop1"));
            baseConfig.AddProperty("prop4", "prop4");
            Assert.AreEqual("prop4", config.GetProperty("prop4"));
            Assert.AreEqual("prop4", prop4.Value);
            baseConfig.SetProperty("prop1", "newvaluefrombase");
            Assert.AreEqual("newvalue", prop1.Value);
            containerConfig.ClearProperty("prop1");
            Assert.AreEqual("newvaluefrombase", config.GetProperty("prop1"));
            Assert.AreEqual("newvaluefrombase", prop1.Value);
            config.SetOverrideProperty("prop2", "overridden");
            config.SetProperty("prop2", "fromContainer");
            Assert.AreEqual("overridden", config.GetProperty("prop2"));
            Assert.AreEqual("overridden", prop2.Value);
            config.clearOverrideProperty("prop2");
            Assert.AreEqual("fromContainer", prop2.Value);
            Assert.AreEqual("fromContainer", config.GetProperty("prop2"));
            config.SetProperty("prop3", "fromContainer");
            Assert.AreEqual("fromContainer", prop3.Value);
            Assert.AreEqual("fromContainer", config.GetProperty("prop3"));
            config.ClearProperty("prop3");
            Assert.AreEqual("prop3", prop3.Value);
            Assert.AreEqual("prop3", config.GetProperty("prop3"));
        }

        [Test]
        public void TestContainerConfiguration()
        {
            ConcurrentCompositeConfiguration config = new ConcurrentCompositeConfiguration();
            Assert.AreEqual(0, config.ContainerConfigurationIndex);
            IConfiguration originalContainerConfig = config.ContainerConfiguration;
            AbstractConfiguration config1 = new ConcurrentDictionaryConfiguration();
            config.AddConfiguration(config1, "base");
            Assert.AreEqual(1, config.ContainerConfigurationIndex);
            config.SetContainerConfigurationIndex(0);
            Assert.AreEqual(0, config.ContainerConfigurationIndex);
            Assert.AreEqual(2, config.NumberOfConfigurations);
            AbstractConfiguration config2 = new ConcurrentDictionaryConfiguration();
            config.AddConfigurationAtIndex(config2, "new", 1);
            AbstractConfiguration config3 = new ConcurrentDictionaryConfiguration();
            config.SetContainerConfiguration(config3, "new container", 2);
            Assert.AreEqual(config3, config.ContainerConfiguration);
            try
            {
                config.SetContainerConfigurationIndex(4);
                Assert.Fail("expect IndexOutOfRangeException");
            }
            catch (IndexOutOfRangeException e)
            {
            }
            try
            {
                config.AddConfigurationAtIndex(new ConcurrentDictionaryConfiguration(), "ignore", 5);
                Assert.Fail("expect IndexOutOfRangeException");
            }
            catch (IndexOutOfRangeException e)
            {
            }
            var list = config.ConfigurationList;
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(originalContainerConfig, list[0]);
            Assert.AreEqual(config2, list[1]);
            Assert.AreEqual(config3, list[2]);
            Assert.AreEqual(config1, list[3]);
            config.RemoveConfiguration(config1);
            Assert.IsFalse(config.GetConfigurationNameList().Contains("base"));
            Assert.IsFalse(config.ConfigurationList.Contains(config1));
            config.RemoveConfigurationAt(1);
            Assert.IsFalse(config.GetConfigurationNameList().Contains("new"));
            Assert.IsFalse(config.ConfigurationList.Contains(config2));
            AbstractConfiguration config4 = new ConcurrentDictionaryConfiguration();
            config.AddConfiguration(config4, "another container");
            config.RemoveConfiguration("another container");
            Assert.IsFalse(config.GetConfigurationNameList().Contains("another container"));
            Assert.IsFalse(config.ConfigurationList.Contains(config4));
        }
    }
}