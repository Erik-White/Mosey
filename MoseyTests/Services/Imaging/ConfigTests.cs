using FluentAssertions;
using NUnit.Framework;
using MoseyTests.AutoData;
using MoseyTests.Extensions;
using AutoFixture.NUnit3;

namespace Mosey.Services.Imaging.Tests
{
    public class ImageFileConfigTests
    {
        public class CloneShould
        {
            [Theory, AutoNSubstituteData]
            public void BeEquivalentToClone(ImageFileConfig originalConfig)
            {
                var clonedConfig = originalConfig.Clone();

                // Not a deep clone
                originalConfig.Should().NotBeSameAs(clonedConfig);
                clonedConfig.Should().BeEquivalentTo(originalConfig);
            }
        }
    }

    public class ScanningDeviceSettingsTests
    {
        public class ConstructorShould
        {
            [Theory, AutoNSubstituteData]
            public void InitializeAllProperties(ScanningDeviceSettings sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
            }

            [Theory, AutoNSubstituteData]
            public void InitializeAllPropertiesWithGreedy([Greedy] ScanningDeviceSettings sut)
            {
                // AutoFixture uses least greedy constructor by default
                sut.AssertAllPropertiesAreNotDefault();
            }
        }

        public class CloneShould
        {
            [Theory, AutoNSubstituteData]
            public void BeEquivalent(ScanningDeviceSettings originalSettings)
            {
                var clonedSettings = originalSettings.Clone();

                // Not a deep clone
                originalSettings.Should().NotBeSameAs(clonedSettings);
                clonedSettings.Should().BeEquivalentTo(originalSettings);
            }
        }
    }
}