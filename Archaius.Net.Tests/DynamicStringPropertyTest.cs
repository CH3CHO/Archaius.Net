using System;
using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class DynamicStringPropertyTest
    {
        private const string NoCallback = "no call back";
        private const string AfterCallback = "after call back";

        private bool m_CallbackFlag;

        public void OnPropertyChanged(object sender, EventArgs args)
        {
            m_CallbackFlag = !m_CallbackFlag;
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            ConfigurationManager.GetConfigInstance().SetProperty("TestProperty", "abc");
        }

        [Test]
        public void TestCallbacksAddUnsubscribe()
        {
            var dp = new DynamicStringProperty("TestProperty", null);
            dp.PropertyChanged += OnPropertyChanged;
            // Trigger callback
            ConfigurationManager.GetConfigInstance().SetProperty("TestProperty", "cde");
            Assert.IsTrue(m_CallbackFlag);
            dp.ClearPropertyChangedHandlers();
            // Trigger callback again
            ConfigurationManager.GetConfigInstance().SetProperty("TestProperty", "def");
            Assert.IsTrue(m_CallbackFlag);
            dp.PropertyChanged += OnPropertyChanged;
            ConfigurationManager.GetConfigInstance().SetProperty("TestProperty", "efg");
            Assert.IsFalse(m_CallbackFlag);
        }
    }
}