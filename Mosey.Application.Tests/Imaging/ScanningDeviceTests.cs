using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AutoFixture.NUnit3;
using DNTScanner.Core;
using FluentAssertions;
using Mosey.Application.Tests.AutoData;
using Mosey.Application.Tests.Customizations;
using Mosey.Core.Imaging;
using Mosey.Tests.Extensions;
using NUnit.Framework;

namespace Mosey.Application.Imaging.Tests
{
    public class ScanningDeviceTests
    {
        public class ConstructorShould
        {
            [Theory, ScanningDeviceAutoData]
            public void InitializeAllProperties(ScanningDevice sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
                sut.IsConnected.Should().BeTrue();
                sut.IsImaging.Should().BeFalse();
            }

            [Theory, ScanningDeviceAutoData]
            public void InitializeAllPropertiesGreedy([Greedy] ScanningDevice sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
                sut.IsConnected.Should().BeTrue();
                sut.IsImaging.Should().BeFalse();
            }
        }

        public class ClearImagesShould
        {
            [Theory, ScanningDeviceAutoData]
            public void EmptyImagesCollection([Frozen] IList<byte[]> images, ScanningDevice sut)
            {
                sut.Images = images;
                sut.Images
                    .Should().NotBeEmpty()
                    .And.BeEquivalentTo(images);

                sut.ClearImages();

                sut.Images
                    .Should().NotBeNull()
                    .And.BeEmpty();
            }
        }

        public class GetImageShould
        {
            [Theory, ScanningDeviceAutoData]
            public void ReturnImagesForSupportedFormat([Frozen] IEnumerable<byte[]> images, [Greedy] ScanningDevice sut)
            {
                sut.GetImage(ScannerSettingsCustomization.SupportedImageFormat);

                sut.Images.Count.Should().Be(images.Count());

                foreach (var (image, deviceImage) in Enumerable.Zip(images, sut.Images))
                {
                    // Image data won't match exactly because headers will differ due to conversion
                    image.Should().IntersectWith(deviceImage);
                }
            }

            [Theory, ScanningDeviceAutoData]
            public void ThrowIfImageFormatNotSupported([Greedy] ScanningDevice sut)
            {
                sut.Invoking(x => x.GetImage(IImagingDevice.ImageFormat.Gif))
                    .Should().Throw<ArgumentException>();
                sut.Images.Should().BeEmpty();
            }

            [Theory, ScanningDeviceAutoData]
            public void ThrowCOMExceptionIfDeviceNotConnected([Greedy] ScanningDevice sut)
            {
                sut.IsConnected = false;

                sut.Invoking(x => x.GetImage(ScannerSettingsCustomization.SupportedImageFormat))
                    .Should().Throw<COMException>();
                sut.Images.Should().BeEmpty();
            }

            [Theory, ScanningDeviceAutoData]
            public void RaiseIsImagingPropertyChanged([Greedy] ScanningDevice sut)
            {
                // IsImaging should only be true during the operation of GetImage()
                sut.IsImaging.Should().BeFalse();

                using (var monitoredSubject = sut.Monitor())
                {
                    monitoredSubject.Subject.GetImage(ScannerSettingsCustomization.SupportedImageFormat);

                    monitoredSubject.Should().RaisePropertyChangeFor(x => x.IsImaging);
                }

                sut.IsImaging.Should().BeFalse();
            }
        }

        public class EqualsShould
        {
            [Theory, ScanningDeviceAutoData]
            public void BeEquivalentToClone([Frozen] ScannerSettings _, ScanningDevice sut, ScanningDevice clone)
                => sut.Equals(clone).Should().BeTrue();

            [Theory, ScanningDeviceAutoData]
            public void EqualSettingsHash([Frozen] ScannerSettings settings, ScanningDevice sut)
            {
                var result = sut.GetHashCode();

                result.Should().Be(settings.Id.GetHashCode());
            }
        }
    }
}