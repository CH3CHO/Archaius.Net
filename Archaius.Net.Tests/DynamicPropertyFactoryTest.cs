using Archaius.Dynamic;
using NUnit.Framework;

namespace Archaius.Net.Tests
{
    [TestFixture]
    public class DynamicPropertyFactoryTest
    {
        [Test]
        public void TestGetSource()
        {
            DynamicPropertyFactory.GetInstance();
            var defaultConfig = DynamicPropertyFactory.BackingConfigurationSource;
            Assert.IsTrue(defaultConfig is ConcurrentCompositeConfiguration);
            Assert.IsTrue(DynamicPropertyFactory.InitializedWithDefaultConfig);
        }
    }
}