using AutoFixture;
using FluentAssertions;
using MoseyTests.Extensions;
using NUnit.Framework;

namespace Mosey.Services.Imaging.Tests
{
    [TestFixture]
    public class ScanningDeviceSettingsTests
    {
        Fixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void ScanningDeviceSettingsTest()
        {
            var instance = _fixture.Create<ScanningDeviceSettings>();

            instance.AssertAllPropertiesAreNotDefault();
        }

        [Test]
        public void ScanningDeviceSettingsTestGreedy()
        {
            // AutoFixture uses least greedy constructor by default
            _fixture.SetGreedyConstructor<ScanningDeviceSettings>();

            var instance = _fixture.Create<ScanningDeviceSettings>();

            instance.AssertAllPropertiesAreNotDefault();
        }

        [Test]
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