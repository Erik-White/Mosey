using AutoFixture;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoseyTests.Extensions;

namespace Mosey.Services.Imaging.Tests
{
    [TestClass()]
    public class ScanningDeviceSettingsTests
    {
        Fixture _fixture;

        [TestInitialize]
        public void SetUp()
        {
            _fixture = new Fixture();
        }

        [TestMethod()]
        public void ScanningDeviceSettingsTest()
        {
            var instance = _fixture.Create<ScanningDeviceSettings>();

            instance.AssertAllPropertiesAreNotDefault();
        }

        [TestMethod()]
        public void ScanningDeviceSettingsTestGreedy()
        {
            // AutoFixture uses least greedy constructor by default
            _fixture.SetGreedyConstructor<ScanningDeviceSettings>();

            var instance = _fixture.Create<ScanningDeviceSettings>();

            instance.AssertAllPropertiesAreNotDefault();
        }

        [TestMethod()]
        public void CloneTest()
        {
            var original = _fixture.Create<ScanningDeviceSettings>();
            var clone = original.Clone();

            // Not a deep clone
            original.Should().NotBeSameAs(clone);
            // Properties and fields should be the same
            clone.Should().BeEquivalentTo(original);
        }
    }
}