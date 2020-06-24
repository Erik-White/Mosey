using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MoseyTests.Extensions;
using Moq;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using DNTScanner.Core;
using Mosey.Models;

namespace Mosey.Services.Imaging.Tests
{
    [TestClass()]
    public class ScanningDevicesTests
    {
        private int CollectionMockCapacity { get; set; } = 5;

        private IFixture _fixture;

        [TestInitialize]
        public void SetUp()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            // Create and configure return values for SystemDevices
            var systemDevicesMock = _fixture.FreezeMoq<ISystemDevices>();
            // Use concrete class otherwise cast from interface proxy type fails
            var scannerDevices = new List<ScanningDevice>();
            _fixture.AddManyTo(scannerDevices, CollectionMockCapacity);
            systemDevicesMock
                .Setup(mock => mock.ScannerDevices(
                    It.IsAny<IImagingDeviceConfig>(),
                    It.IsAny<int>()
                    ))
                .Returns(scannerDevices);

            var scannerSettings = new List<ScannerSettings>();
            _fixture.AddManyTo(scannerSettings, CollectionMockCapacity);
            // Ensure there is an item with a predictable ID
            scannerSettings.First().Id = CollectionMockCapacity.ToString();
            systemDevicesMock
                .Setup(mock => mock.ScannerSettings(
                    It.IsAny<int>()
                    ))
                .Returns(scannerSettings);
        }

        [TestMethod()]
        public void ScanningDevicesTest()
        {
            var instance = _fixture.Create<ScanningDevices>();

            instance.AssertAllPropertiesAreNotDefault();
            instance.IsEmpty.Should().BeFalse();
            instance.Devices.Count().Should().Be(CollectionMockCapacity);
        }

        [TestMethod()]
        public void ScanningDevicesGreedyTest()
        {
            _fixture.SetGreedyConstructor<ScanningDevices>();
            var instance = _fixture.Create<ScanningDevices>();

            instance.AssertAllPropertiesAreNotDefault();
            instance.IsEmpty.Should().BeFalse();
            instance.Devices.Count().Should().Be(CollectionMockCapacity);
        }

        [TestMethod()]
        public void AddDeviceByIDTest()
        {
            var instance = _fixture.Create<ScanningDevices>();
            string predictableID = CollectionMockCapacity.ToString();

            // The deviceID parameter is checked against IDs returned by ISystemDevices.ScannerSettings()
            // The deviceID will not already be in the collection as it is initialised from
            // SystemDevices.ScannerDevices() which will return mocked devices
            instance.AddDevice(deviceID: predictableID);

            instance.Devices.Should()
                .HaveCount(CollectionMockCapacity + 1)
                .And.OnlyHaveUniqueItems()
                .And.Contain(i => i.DeviceID == predictableID);
        }

        [TestMethod()]
        public void AddDeviceInstanceTest()
        {
            var instance = _fixture.Create<ScanningDevices>();
            // Use concrete class otherwise cast from interface proxy type fails
            var device = _fixture.Create<ScanningDevice>();

            instance.AddDevice(device);

            instance.Devices.Should()
                .HaveCount(CollectionMockCapacity + 1)
                .And.Contain(device);
        }

        [TestMethod()]
        public void AddDeviceInstanceRaisesArgumentExceptionTest()
        {
            var instance = _fixture.Create<ScanningDevices>();
            // Use concrete class otherwise cast from interface proxy type fails
            var device = _fixture.Create<ScanningDevice>();

            // Ensure the device already exists in the collection
            instance.AddDevice(device);

            instance
                .Invoking(i => i.AddDevice(device))
                .Should().Throw<ArgumentException>();
        }

        [TestMethod()]
        public void DisableAllTest()
        {
            var instance = _fixture.Create<ScanningDevices>();

            instance.Devices.All(device => device.IsEnabled == true).Should().BeTrue();

            instance.DisableAll();

            instance.Devices.All(device => device.IsEnabled == false).Should().BeTrue();
        }

        [TestMethod()]
        public void EnableAllTest()
        {
            var instance = _fixture.Create<ScanningDevices>();

            foreach (var device in instance.Devices)
            {
                device.IsEnabled = false;
            }

            instance.Devices.All(device => device.IsEnabled == false).Should().BeTrue();

            instance.EnableAll();

            instance.Devices.All(device => device.IsEnabled == true).Should().BeTrue();
        }

        [TestMethod()]
        [DataRow(true)]
        [DataRow(false)]
        public void GetByEnabledTest(bool enabled)
        {
            var instance = _fixture.Create<ScanningDevices>();
            var device = instance.Devices.First();

            device.IsEnabled = false;

            var results = instance.GetByEnabled(enabled);

            if (enabled)
            {
                results.Should()
                    .HaveCount(CollectionMockCapacity - 1)
                    .And.NotContain(device);
            }
            else
            {
                results.Should().OnlyContain(d => d == device);
            }
        }

        [TestMethod()]
        public void RefreshDevicesDisconnectedAllTest()
        {
            var instance = _fixture.Create<ScanningDevices>();

            // The mock SystemDevices.ScannerProperties() will return nothing
            // So the existing devices will be marked as disconnected
            instance.RefreshDevices();

            instance.Devices.All(device => device.IsConnected == false).Should().BeTrue();
        }

        [TestMethod()]
        public void RefreshDevicesDisconnectedSingleTest()
        {
            var systemDevices = _fixture.Create<ISystemDevices>();
            var systemDevicesMock = Mock.Get(systemDevices);

            var instance = _fixture.Create<ScanningDevices>();
            var initalDevices = instance.Devices;

            // Get the existing DeviceIDs and return from ISystemDevices.ScannerProperties()
            // Remove one DeviceID so one device will appear disconnected
            var deviceIDs = instance.Devices.Select(device => device.DeviceID).ToList();
            var scannerProperties = new List<IDictionary<string, object>>();
            foreach (var deviceID in deviceIDs.Take(deviceIDs.Count - 1))
            {
                scannerProperties.Add(new Dictionary<string, object>() { { "Unique Device ID", deviceID } });
            }
            systemDevicesMock
                .Setup(mock => mock.ScannerProperties(It.IsAny<int>()))
                .Returns(scannerProperties);

            instance.RefreshDevices();

            // All devices should be connected except the last
            instance.Devices
                .Should().HaveCount(CollectionMockCapacity)
                .And.BeEquivalentTo(initalDevices);
            instance.Devices
                .Take(instance.Devices.Count() - 1)
                .All(device => device.IsConnected == true)
                .Should().BeTrue();
            instance.Devices
                .Last()
                .IsConnected
                .Should().BeFalse();
        }

        [TestMethod()]
        [DataRow(true)]
        [DataRow(false)]
        public void SetDeviceEnabledTest(bool enabled)
        {
            var instance = _fixture.Create<ScanningDevices>();
            var device = instance.Devices.First();

            instance.SetDeviceEnabled(device.DeviceID, enabled);

            instance.Devices
                .Where(d => d.DeviceID == device.DeviceID)
                .Select(d => d.IsEnabled)
                .Should().Equal(enabled);
        }

        [TestMethod()]
        [DataRow(true)]
        [DataRow(false)]
        public void SetDeviceEnabledInstanceTest(bool enabled)
        {
            var instance = _fixture.Create<ScanningDevices>();
            var device = instance.Devices.First();

            instance.SetDeviceEnabled(device, enabled);

            instance.Devices
                .Where(d => d.DeviceID == device.DeviceID)
                .Select(d => d.IsEnabled)
                .Should().Equal(enabled);
        }
    }
}