using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Runtime.InteropServices;
using AutoFixture.NUnit3;
using DNTScanner.Core;
using FluentAssertions;
using Mosey.Models.Imaging;
using Mosey.Tests.AutoData;
using Mosey.Tests.Customizations;
using Mosey.Tests.Extensions;
using NUnit.Framework;

namespace Mosey.Services.Imaging.Tests
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

        public class SaveImageShould
        {
            private readonly string filename = "Filename";
            private readonly string directory = new MockFileSystem().Path.Combine("C:", "Directory");

            [Theory, ScanningDeviceAutoData]
            public void ThrowInvalidOperationExceptionIfNoImages([Greedy] ScanningDevice sut)
            {
                sut.Images = null;

                sut
                    .Invoking(i => i.SaveImage(filename, directory, ScannerSettingsCustomization.SupportedImageFormat))
                    .Should().Throw<InvalidOperationException>();
            }

            [Theory, ScanningDeviceAutoData]
            public void ThrowArgumentExceptionIfNoFilePath([CollectionSize(1)] IList<byte[]> images, [Greedy] ScanningDevice sut)
            {
                sut.Images = images;

                sut
                    .Invoking(i => i.SaveImage(string.Empty, string.Empty, ScannerSettingsCustomization.SupportedImageFormat))
                    .Should().Throw<ArgumentException>()
                    .WithMessage("A valid filename and directory must be supplied");
            }

            [Theory, ScanningDeviceAutoData]
            public void SaveImageWithFilePath([CollectionSize(1)] IList<byte[]> images, [Frozen] IFileSystem fileSystem, [Greedy] ScanningDevice sut)
            {
                sut.Images = images;

                var result = sut.SaveImage(filename, directory, ScannerSettingsCustomization.SupportedImageFormat);

                result.Should().HaveCount(images.Count);
                (fileSystem as MockFileSystem).AllFiles.Should().BeEquivalentTo(result);
                foreach (var filePath in result)
                {
                    var expectedPath = fileSystem.Path.Combine(directory, filename);
                    expectedPath = fileSystem.Path.ChangeExtension(expectedPath, ScannerSettingsCustomization.SupportedImageFormat.ToString());
                    filePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
                }
            }

            [Theory, ScanningDeviceAutoData]
            public void AppendCountToFilePaths([CollectionSize(2)] IList<byte[]> images, [Frozen] IFileSystem fileSystem, [Greedy] ScanningDevice sut)
            {
                var count = 0;
                sut.Images = images;

                var result = sut.SaveImage(filename, directory, ScannerSettingsCustomization.SupportedImageFormat);

                result.Should().HaveCount(images.Count);
                (fileSystem as MockFileSystem).AllFiles.Should().BeEquivalentTo(result);
                foreach (var filePath in result)
                {
                    var expectedPath = fileSystem.Path.Combine(directory, $"{filename}_{++count}");
                    expectedPath = fileSystem.Path.ChangeExtension(expectedPath, ScannerSettingsCustomization.SupportedImageFormat.ToString());
                    filePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
                }
            }

            [Theory, ScanningDeviceAutoData]
            public void WriteImageDataToDisk([CollectionSize(2)] IList<byte[]> images, [Frozen] IFileSystem fileSystem, [Greedy] ScanningDevice sut)
            {
                var fs = fileSystem as MockFileSystem;
                sut.Images = images;

                var result = sut.SaveImage(filename, directory, ScannerSettingsCustomization.SupportedImageFormat);

                result.Should().HaveCount(images.Count);
                fs.AllFiles.Should().BeEquivalentTo(result);
                foreach (var (filePath, image) in Enumerable.Zip(fs.AllFiles, images))
                {
                    // Image data won't match exactly because headers will differ due to conversion
                    fs.GetFile(filePath).Contents.Should().IntersectWith(image);
                }
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