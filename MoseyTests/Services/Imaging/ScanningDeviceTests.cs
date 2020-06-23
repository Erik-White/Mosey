using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Moq;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using System.IO.Abstractions;
using MoseyTests;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging.Tests
{
    [TestClass()]
    public class ScanningDeviceTests
    {
        private IFixture _fixture;
        private readonly IList<byte[]> _images = new List<byte[]>();
        private DNTScanner.Core.ScannerSettings _settings;
        private ScanningDevice.ImageFormat _supportedFormat = ScanningDevice.ImageFormat.Jpeg;

        [TestInitialize]
        public void SetUp()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Mock a ScannerSettings instance to allow value comparisons
            _settings = _fixture.Freeze<DNTScanner.Core.ScannerSettings>();
            // Ensure that there is at least one real SupportedTransferFormat
            _settings
                .SupportedTransferFormats
                .Add(_supportedFormat.ToWIAImageFormat().Value.ToString(), "SupportedWIAImageFormat");

            // Mock images returned from scanner
            _fixture.AddManyTo(_images, 5);

            // Create and configure return values for SystemDevices
            var systemDevicesMock = _fixture.FreezeMoq<ISystemDevices>();
            systemDevicesMock
                .Setup(mock => mock.PerformScan(
                    It.IsAny<DNTScanner.Core.ScannerSettings>(),
                    It.IsAny<IImagingDeviceConfig>(),
                    It.IsAny<ScanningDevice.ImageFormat>()
                    ))
                .Returns(_images);
            systemDevicesMock
                .Setup(mock => mock.PerformScan(
                    It.IsAny<DNTScanner.Core.ScannerSettings>(),
                    It.IsAny<IImagingDeviceConfig>(),
                    It.IsAny<ScanningDevice.ImageFormat>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                    ))
                .Returns(_images);

            // Ensure ScanningDevice dependencies are mocked
            _fixture.Register<IFileSystem>(() => new System.IO.Abstractions.TestingHelpers.MockFileSystem());
            var scanningDeviceMock = new Mock<ScanningDevice>(
                _settings,
                _fixture.Create<IImagingDeviceConfig>(),
                _fixture.Create<ISystemDevices>(),
                _fixture.Create<IFileSystem>()
                )
            {
                CallBase = true
            };
            // Intercept SaveImageToDisk() to ensure we don't touch file system
            scanningDeviceMock.Setup(m => m.SaveImageToDisk(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<ScanningDevice.ImageFormat>(),
                It.IsAny<System.Drawing.Imaging.EncoderParameters>()
                ))
                .Verifiable();
            _fixture.Inject(scanningDeviceMock.Object);
        }

        [TestMethod()]
        public void ScanningDeviceTest()
        {
            // The SetUp fixture uses the greedy constructor so use a new fixture
            // AutoFixture uses least greedy constructor by default 
            var instance = new Fixture()
                .Customize(new AutoMoqCustomization())
                .Create<ScanningDevice>();

            instance.AssertAllPropertiesAreNotDefault();
            instance.IsConnected.Should().BeTrue();
            instance.IsEnabled.Should().BeFalse();
            instance.IsImaging.Should().BeFalse();
        }

        [TestMethod()]
        public void ScanningDeviceTestGreedy()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.AssertAllPropertiesAreNotDefault();
            instance.IsConnected.Should().BeTrue();
            instance.IsEnabled.Should().BeFalse();
            instance.IsImaging.Should().BeFalse();
        }

        [TestMethod()]
        public void ClearImagesTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.Images = _images;

            instance.Images.Should().NotBeEmpty().And.HaveCount(_images.Count);

            instance.ClearImages();

            instance.Images.Should().NotBeNull().And.BeEmpty();
        }

        [TestMethod()]
        public void GetImageTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.GetImage(_supportedFormat);

            instance.Images.Should().BeEquivalentTo(_images);
        }

        [TestMethod()]
        public void GetImageRaisesCOMExceptionTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.IsConnected = false;

            instance
                .Invoking(i => i.GetImage())
                .Should().Throw<COMException>();
        }

        [TestMethod()]
        public void GetImageRaisesArgumentExceptionTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance
                .Invoking(i => i.GetImage())
                .Should().Throw<ArgumentException>();
        }

        [TestMethod()]
        public void GetImageRaisesPropertyChangedTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            // IsImaging should only be true during the operation of GetImage()
            instance.IsImaging.Should().BeFalse();

            using (var monitoredSubject = instance.Monitor())
            {
                monitoredSubject.Subject.GetImage(_supportedFormat);

                monitoredSubject.Should().RaisePropertyChangeFor(x => x.IsImaging);
            }

            instance.IsImaging.Should().BeFalse();
        }

        [TestMethod()]
        public void SaveImageTest()
        {
            var instance = Mock.Get(_fixture.Create<ScanningDevice>());

            instance.Object.Images = _images;

            var result = instance.Object.SaveImage("FileName", "Directory", _supportedFormat);

            result.Should().HaveCount(_images.Count);
            instance.Verify();
        }

        [TestMethod()]
        public void SaveImageStringFormatTest()
        {
            var instance = Mock.Get(_fixture.Create<ScanningDevice>());

            instance.Object.Images = _images;

            var result = instance.Object.SaveImage("FileName", "Directory", _supportedFormat);

            result.Should().HaveCount(_images.Count);
            instance.Verify();
        }

        [TestMethod()]
        public void SaveImageRaisesInvalidOperationExceptionTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.Images = null;

            instance
                .Invoking(i => i.SaveImage(string.Empty, string.Empty, _supportedFormat))
                .Should().Throw<InvalidOperationException>();
        }

        [TestMethod()]
        public void SaveImageRaisesArgumentExceptionTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.Images = _images;

            instance
                .Invoking(i => i.SaveImage(string.Empty, string.Empty, _supportedFormat.ToString()))
                .Should().Throw<ArgumentException>();
        }

        [TestMethod()]
        public void EqualsDeviceTest()
        {
            var instance = _fixture.Create<ScanningDevice>();
            var compare = new Fixture()
                .Customize(new AutoMoqCustomization())
                .Create<ScanningDevice>();

            instance.Equals(instance).Should().BeTrue();
            instance.Equals(compare).Should().BeFalse();
        }

        [TestMethod()]
        public void EqualsObjectTest()
        {
            var instance = _fixture.Create<ScanningDevice>();
            var compare = new Fixture()
                .Customize(new AutoMoqCustomization())
                .Create<ScanningDevice>();

            instance.Equals((object)instance).Should().BeTrue();
            instance.Equals((object)compare).Should().BeFalse();
        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            var instance = _fixture.Create<ScanningDevice>();

            instance.GetHashCode().Should().Be(_settings.Id.GetHashCode());
        }
    }
}