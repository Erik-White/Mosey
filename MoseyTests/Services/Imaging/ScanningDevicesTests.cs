using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using AutoFixture.NUnit3;
using Moq;
using FluentAssertions;
using DNTScanner.Core;
using Mosey.Models;
using MoseyTests.Extensions;
using MoseyTests.AutoData;

namespace Mosey.Services.Imaging.Tests
{
    [TestFixture]
    public class ScanningDevicesTests
    {
        public class ConstructorShould
        {
            [Theory, ScanningDeviceAutoData]
            public void InitializeCollection(ScanningDevices sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
                sut.IsEmpty.Should().BeFalse();
                sut.Devices.Count().Should().Be(3);
                sut.Devices.All(device => device.IsEnabled == true).Should().BeTrue();
            }

            [Theory, ScanningDeviceAutoData]
            public void InitializeCollectionGreedy([Greedy] ScanningDevices sut)
            {
                sut.AssertAllPropertiesAreNotDefault();
                sut.IsEmpty.Should().BeFalse();
                sut.Devices.Count().Should().Be(3);
                sut.Devices.All(device => device.IsEnabled == true).Should().BeTrue();
            }
        }

        public class AddDeviceShould
        {
            [Theory, ScanningDeviceAutoData]
            public void ThrowIfDeviceIdExists([Frozen, CollectionSize(5)] IEnumerable<ScannerSettings> scannerSettings, ScanningDevices sut)
            {
                var existingId = scannerSettings.First().Id;

                sut.Invoking(x => x.AddDevice(deviceID: existingId))
                    .Should().Throw<ArgumentException>();
                sut.Devices.Should()
                    .HaveCount(5)
                    .And.OnlyHaveUniqueItems()
                    .And.Contain(i => i.DeviceID == existingId);
            }

            [Theory, ScanningDeviceAutoData]
            public void ThrowIfDeviceInstanceExists(
                [Frozen] IImagingDeviceConfig deviceConfig,
                [Frozen] ScannerSettings settings,
                [Frozen, CollectionSize(1)] IEnumerable<ScannerSettings> scannerSettings,
                ScanningDevices sut)
            {
                var existingInstance = new ScanningDevice(settings, deviceConfig);

                sut.Invoking(x => x.AddDevice(existingInstance))
                    .Should().Throw<ArgumentException>();
                sut.Devices.Should()
                    .HaveCount(1)
                    .And.OnlyHaveUniqueItems()
                    .And.Contain(i => i.DeviceID == existingInstance.DeviceID);
            }

            [Theory, ScanningDeviceAutoData]
            public void ContainUniqueDevices(
                ScanningDevice device,
                [Frozen] IImagingDeviceConfig deviceConfig,
                [Frozen, CollectionSize(5)] IEnumerable<ScannerSettings> scannerSettings,
                ScanningDevices sut)
            {
                var scanningDevices = scannerSettings.Select(x => new ScanningDevice(x, deviceConfig));

                sut.AddDevice(device);

                sut.Devices.Should()
                    .HaveCount(6)
                    .And.OnlyHaveUniqueItems()
                    .And.Contain(scanningDevices)
                    .And.Contain(i => i.DeviceID == device.DeviceID);
            }
        }

        public class DisableAllShould
        {
            [Theory, ScanningDeviceAutoData]
            public void SetAllDevicesToDisabled(ScanningDevices sut)
            {
                sut.Devices.All(device => device.IsEnabled).Should().BeTrue();

                sut.DisableAll();

                sut.Devices.All(device => !device.IsEnabled).Should().BeTrue();
            }
        }

        public class EnableAllShould
        {
            [Theory, ScanningDeviceAutoData]
            public void SetAllDevicesToEnabled(ScanningDevices sut)
            {
                foreach (var device in sut.Devices)
                {
                    device.IsEnabled = false;
                }
                sut.Devices.All(device => !device.IsEnabled).Should().BeTrue();

                sut.EnableAll();

                sut.Devices.All(device => device.IsEnabled).Should().BeTrue();
            }
        }

        public class RefreshDevicesShould
        {
            [Theory, ScanningDeviceAutoData]
            public void DisconnectAllDevicesIfNotFound(ScanningDevices sut)
            {
                sut.RefreshDevices();

                sut.Devices.All(device => !device.IsConnected).Should().BeTrue();
            }

            [Theory, ScanningDeviceAutoData]
            public void DisconnectSingleDeviceIfNotFound(
                [Frozen] Mock<ISystemDevices> systemDevices,
                [Frozen] IEnumerable<ScannerSettings> scannerSettings)
            {
                systemDevices
                    .Setup(mock => mock.ScannerSettings(It.IsAny<int>()))
                    .Returns(scannerSettings);
                var sut = new ScanningDevices(systemDevices.Object);
                var initalDevices = sut.Devices;

                // Add the existing DeviceIds except one so the device will appear disconnected
                var scannerProperties = new List<IDictionary<string, object>>();
                foreach (var deviceID in sut.Devices.Skip(1).Select(d => d.DeviceID))
                {
                    scannerProperties.Add(new Dictionary<string, object>() { { "Unique Device ID", deviceID } });
                }
                systemDevices
                    .Setup(mock => mock.ScannerProperties(It.IsAny<int>()))
                    .Returns(scannerProperties);

                sut.RefreshDevices();

                sut.Devices
                    .Should().HaveCount(3)
                    .And.BeEquivalentTo(initalDevices);
                sut.Devices
                    .Skip(1).All(device => device.IsConnected)
                    .Should().BeTrue();
                sut.Devices
                    .First().IsConnected
                    .Should().BeFalse();
            }

            [Theory, ScanningDeviceAutoData]
            public void AddNewDevicesToCollection(
                [Frozen] Mock<ISystemDevices> systemDevices,
                [Frozen, CollectionSize(4)] IEnumerable<ScannerSettings> scannerSettings,
                ScannerSettings newScannerSettings)
            {
                systemDevices
                    .Setup(mock => mock.ScannerSettings(It.IsAny<int>()))
                    .Returns(scannerSettings);
                var sut = new ScanningDevices(systemDevices.Object);
                var initalDevices = sut.Devices;

                // Add an extra scanner to the collection
                scannerSettings = scannerSettings.Append(newScannerSettings);
                var scannerProperties = new List<IDictionary<string, object>>();
                foreach (var deviceID in sut.Devices.Select(d => d.DeviceID).Append(newScannerSettings.Id))
                {
                    scannerProperties.Add(new Dictionary<string, object>() { { "Unique Device ID", deviceID } });
                }
                systemDevices
                    .Setup(mock => mock.ScannerProperties(It.IsAny<int>()))
                    .Returns(scannerProperties);
                systemDevices
                    .Setup(mock => mock.ScannerSettings(It.IsAny<int>()))
                    .Returns(scannerSettings);

                sut.RefreshDevices();

                sut.Devices
                    .Should().HaveCount(5)
                    .And.Contain(initalDevices);
                sut.Devices
                    .Any(d => d.DeviceID == newScannerSettings.Id)
                    .Should().BeTrue();
            }
        }

        public class SetDeviceEnabledShould
        {
            [Theory, ScanningDeviceAutoData]
            public void EnableCorrectDevice(ScanningDevices sut)
            {
                var deviceId = sut.Devices.First().DeviceID;
                foreach (var device in sut.Devices)
                {
                    device.IsEnabled = false;
                }

                sut.SetDeviceEnabled(deviceId, true);

                sut.Devices.First().IsEnabled.Should().BeTrue();
                sut.Devices.Skip(1).All(d => !d.IsEnabled).Should().BeTrue();
            }

            [Theory, ScanningDeviceAutoData]
            public void DisableCorrectDevice(ScanningDevices sut)
            {
                var deviceId = sut.Devices.First().DeviceID;
                foreach (var device in sut.Devices)
                {
                    device.IsEnabled = true;
                }

                sut.SetDeviceEnabled(deviceId, true);

                sut.Devices.First().IsEnabled.Should().BeTrue();
                sut.Devices.Skip(1).All(d => d.IsEnabled).Should().BeTrue();
            }
        }
    }
}