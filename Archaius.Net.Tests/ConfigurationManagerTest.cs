using System;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class ConfigurationManagerTest
    {
        private static readonly DynamicStringProperty m_Prop1 = DynamicPropertyFactory.GetInstance().GetStringProperty("prop1", null);

        [Test]
        public void TestInstall()
        {
            ConfigurationManager.GetConfigInstance().SetProperty("prop1", "abc");
            Assert.AreEqual("abc", ConfigurationManager.GetConfigInstance().GetProperty("prop1"));
            Assert.AreEqual("abc", m_Prop1.Value);
            ConcurrentDictionaryConfiguration newConfig = new ConcurrentDictionaryConfiguration();
            newConfig.SetProperty("prop1", "fromNewConfig");
            ConfigurationManager.Install(newConfig);
            Assert.AreEqual("fromNewConfig", ConfigurationManager.GetConfigInstance().GetProperty("prop1"));
            Assert.AreEqual("fromNewConfig", m_Prop1.Value);
            newConfig.SetProperty("prop1", "changed");
            Assert.AreEqual("changed", ConfigurationManager.GetConfigInstance().GetProperty("prop1"));
            Assert.AreEqual("changed", m_Prop1.Value);
            try
            {
                ConfigurationManager.Install(new ConcurrentDictionaryConfiguration());
                Assert.Fail("InvalidOperationException expected");
            }
            catch (InvalidOperationException e)
            {
            }
            try
            {
                DynamicPropertyFactory.InitWithConfigurationSource(new ConcurrentDictionaryConfiguration());
                Assert.Fail("InvalidOperationException expected");
            }
            catch (InvalidOperationException e)
            {
            }
        }
    }
}