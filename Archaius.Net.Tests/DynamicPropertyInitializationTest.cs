using System;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class DynamicPropertyInitializationTest
    {
        [Test]
        public void TestDefaultConfig()
        {
            Environment.SetEnvironmentVariable("xyz", "fromSystem");
            DynamicStringProperty prop = new DynamicStringProperty("xyz", null);
            Assert.IsNotNull(DynamicPropertyFactory.BackingConfigurationSource);
            Assert.AreEqual("fromSystem", prop.Value);

            object lastModified = null;
            EventHandler<ConfigurationEventArgs> handler = (sender, args) =>
                                                           {
                                                               if (!args.BeforeOperation)
                                                               {
                                                                   lastModified = args.Value;
                                                               }
                                                           };
            ConfigurationManager.GetConfigInstance().ConfigurationChanged += handler;

            try
            {
                // Because environment variables default to higher priority than application settings, this set will no-op
                ConfigurationManager.GetConfigInstance().SetProperty("xyz", "override");
                Assert.AreEqual("fromSystem", prop.Value);
                Assert.AreEqual(null, lastModified);

                var newConfig = new ConcurrentDictionaryConfiguration();
                newConfig.SetProperty("xyz", "fromNewConfig");
                ConfigurationManager.Install(newConfig);
                Assert.AreEqual("fromNewConfig", prop.Value);
                ConfigurationManager.GetConfigInstance().SetProperty("xyz", "new");
                Assert.AreEqual("new", lastModified);
                Assert.AreEqual("new", prop.Value);
                Assert.AreEqual(3, newConfig.ConfigurationChangedEventHandlers.Length);
            }
            catch (Exception ex)
            {
                ConfigurationManager.GetConfigInstance().ConfigurationChanged -= handler;
            }
        }
    }
}