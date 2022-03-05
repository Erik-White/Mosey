using System;
using System.Collections.Generic;
using System.Linq;
using DNTScanner.Core;
using Mosey.Models;
using Mosey.Models.Imaging;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// A persistent collection of WIA enabled scanners.
    /// Items are not removed from the collection when it is refreshed.
    /// Instead, their status is updated to indicate that they may not be connected.
    /// </summary>
    public class ScanningDevices : IImagingDevices<IImagingDevice>
    {
        private readonly ObservableItemsCollection<ScanningDevice> _devices = new();
        private readonly ISystemImagingDevices<ScannerSettings> _systemDevices;

        /// <summary>
        /// A collection of <see cref="ScanningDevice"/>s, representing physical scanners.
        /// </summary>
        public IEnumerable<IImagingDevice> Devices => _devices;

        public bool IsEmpty
            => !_devices.Any();

        public bool IsImagingInProgress
            => _devices.Any(x => x.IsImaging is true);

        /// <summary>
        /// Initialize the collection <see cref="ScanningDevice"/>s with the specified <paramref name="deviceConfig"/>.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the collection's <see cref="ScanningDevice"/>s</param>
        /// <param name="systemDevices">An <see cref="ISystemImagingDevices"/> instance that provide access to the WIA driver devices</param>
        public ScanningDevices(ImagingDeviceConfig deviceConfig = null, ISystemImagingDevices<ScannerSettings> systemDevices = null)
        {
            _systemDevices = systemDevices ?? new DntScannerDevices();
            GetDevices(deviceConfig ?? new ImagingDeviceConfig());
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/>, or <see langword="null"/> if no matching device can be found</returns>
        public ScanningDevice AddDevice(string deviceID)
            => AddDevice(deviceID, null);

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">If a device with the same <see cref="IDevice.DeviceID"/> already exists in the collection</exception>
        public void AddDevice(IDevice device)
            => AddDevice((ScanningDevice)device);

        /// <summary>
        /// Add a <see cref="ScanningDevice"/> to the collection.
        /// </summary>
        /// <param name="device">A <see cref="ScanningDevice"/> instance</param>
        /// <exception cref="ArgumentException">If a device with the same <see cref="IDevice.DeviceID"/> already exists in the collection</exception>
        public void AddDevice(ScanningDevice device)
        {
            if (!_devices.Contains(device))
            {
                _devices.Add(device);
            }
            else
            {
                throw new ArgumentException($"This device {device.Name}, ID #{device.ID} already exists.");
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <param name="config"><see cref="IImagingDeviceConfig"/> settings for the returned <see cref="ScanningDevice"/></param>
        /// <param name="connectRetries">The number of attempts to retry connecting to the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/>, or <see langword="null"/> if no matching device can be found</returns>
        public ScanningDevice AddDevice(string deviceID, ImagingDeviceConfig config)
        {
            ScanningDevice device = null;

            // Attempt to connect a device matching the deviceID
            var settings = _systemDevices.GetDeviceSettings().FirstOrDefault(x => x.Id == deviceID);

            if (settings is not null)
            {
                device = new ScanningDevice(settings, config);
                AddDevice(device);
            }

            return device;
        }

        public void DisableAll() => SetByEnabled(false);

        public void EnableAll() => SetByEnabled(true);

        /// <summary>
        /// Retrieve <see cref="ScanningDevice"/>s that are connected to the system and add them to the collection.
        /// Update the status of any devices are already present in the collection.
        /// </summary>
        public void RefreshDevices() => RefreshDevices(new ImagingDeviceConfig());

        /// <inheritdoc cref="RefreshDevices()"/>
        public void RefreshDevices(ImagingDeviceConfig deviceConfig, bool enableDevices = true)
        {
            const string DEVICE_ID_KEY = "Unique Device ID";
            var deviceIds = new List<string>();

            // Check the static properties for changed IDs and only retrieve ScannerSetting instances if necessary.
            // This saves connecting the devices via the WIA driver and is much faster
            var deviceProperties = _systemDevices.GetDeviceProperties();
            if (deviceProperties is null || !deviceProperties.Any())
            {
                // No devices detected, any current devices have been disconnected
                foreach (ScanningDevice device in _devices)
                {
                    device.IsConnected = false;
                }

                return;
            }

            // Check if devices not already in the collection
            // Or already in the collection, but not connected
            var currentDevices = Devices.Select(d => d.DeviceID);
            foreach (var properties in deviceProperties)
            {
                if (properties.TryGetValue(DEVICE_ID_KEY, out var id))
                {
                    deviceIds.Add((string)id);
                }
            }

            // These devices can no longer be found and are disconnected
            foreach (var deviceId in currentDevices.Except(deviceIds))
            {
                var device = _devices.FirstOrDefault(d => d.DeviceID == deviceId);
                device.IsConnected = false;
            }

            // These are new devices, add them to the collection
            foreach (var deviceId in deviceIds.Except(currentDevices))
            {
                var device = AddDevice(deviceId, deviceConfig);
                device.IsEnabled = enableDevices;
            }

            // These devices are already in the collection, but previously disconnected
            foreach (var deviceId in currentDevices.Intersect(deviceIds))
            {
                var existingDevice = _devices.FirstOrDefault(d => d.DeviceID == deviceId && !d.IsConnected);
                if (existingDevice is not null)
                {
                    // Remove the existing device and replace with the updated
                    var enabled = existingDevice.IsEnabled;
                    _devices.Remove(existingDevice);

                    var device = AddDevice(deviceId, deviceConfig);
                    device.IsEnabled = enabled;
                }
            }
        }

        /// <summary>
        /// A collection of <see cref="IImagingDevice"/> instances representing physical devices connected to the system.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the <see cref="ScanningDevice"/> instances</param>
        /// <returns>A collection of <see cref="ScanningDevice"/> instances representing physical scanning devices connected to the system</returns>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        private IEnumerable<IImagingDevice> ScannerDevices(ImagingDeviceConfig deviceConfig)
        {
            foreach (var settings in _systemDevices.GetDeviceSettings())
            {
                yield return new ScanningDevice(settings, deviceConfig);
            }
        }

        /// <summary>
        /// Retrieve <see cref="ScanningDevice"/>s that are connected to the system and add them to the collection.
        /// The exisiting items in the collection are cleared first.
        /// The specified <paramref name="deviceConfig"/> is used to initialize the devices.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the collection's <see cref="ScanningDevice"/>s</param>
        /// <returns>The number of devices added to the collection</returns>
        private void GetDevices(ImagingDeviceConfig deviceConfig)
        {
            deviceConfig ??= new ImagingDeviceConfig(default, default, default);

            // Populate a new collection of scanners using specified image settings
            _devices.Clear();
            foreach (var device in ScannerDevices(deviceConfig).Select(d => d as ScanningDevice))
            {
                device.IsEnabled = true;
                AddDevice(device);
            }
        }

        /// <summary>
        /// Set the <see cref="ScanningDevice.IsEnabled"/> property on all devices in the collection.
        /// </summary>
        /// <param name="enabled">Sets the <see cref="ScanningDevice.IsEnabled"/> property</param>
        private void SetByEnabled(bool enabled)
        {
            // Set the IsEnabled property on all members of the collection
            foreach (var device in _devices)
            {
                device.IsEnabled = enabled;
            }
        }
    }
}