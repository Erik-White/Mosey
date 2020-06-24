using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Mosey.Services.Imaging;
using MoseyTests.Extensions;

namespace Mosey.Services.Imaging.Tests
{
    [TestClass()]
    public class ScanningDevicesTests
    {
        private IFixture _fixture;

        [TestInitialize]
        public void SetUp()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Create and configure return values for SystemDevices
            var systemDevicesMock = _fixture.FreezeMoq<ISystemDevices>();
            /*
            systemDevicesMock
                .Setup(mock => mock.PerformScan(
                    It.IsAny<DNTScanner.Core.ScannerSettings>(),
                    It.IsAny<IImagingDeviceConfig>(),
                    It.IsAny<ScanningDevice.ImageFormat>()
                    ))
                .Returns(_images);
            */
        }

        [TestMethod()]
        public void ScanningDevicesTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ScanningDevicesTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddDeviceTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddDeviceTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddDeviceTest2()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddDeviceTest3()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DisableAllTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EnableAllTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetByEnabledTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RefreshDevicesTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RefreshDevicesTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetDeviceEnabledTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetDeviceEnabledTest1()
        {
            Assert.Fail();
        }
    }
}