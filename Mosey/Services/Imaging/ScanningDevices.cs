using System;
using System.Collections.Generic;
using System.Linq;
using Mosey.Models;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// A persistent collection of WIA enabled scanners
    /// Items are not removed from the collection when it is refreshed
    /// Instead, their status is updated to indicate that they may not be connected
    /// </summary>
    public class ScanningDevices : IImagingDevices<IImagingDevice>
    {
        private readonly ICollection<IImagingDevice> _devices = new ObservableItemsCollection<IImagingDevice>();
        private readonly ISystemDevices _systemDevices;

        /// <summary>
        /// The number of time to attempt reconnection to the WIA driver.
        /// </summary>
        public int ConnectRetries { get; set; } = 5;

        /// <summary>
        /// A collection of <see cref="ScanningDevice"/>s, representing physical scanners.
        /// </summary>
        public IEnumerable<IImagingDevice> Devices => _devices;

        public bool IsEmpty => !_devices.Any();
        public bool IsImagingInProgress => _devices.Any(x => x.IsImaging is true);

        /// <summary>
        /// Initialize an empty collection.
        /// </summary>
        public ScanningDevices() : this(null, null) { }

        /// <summary>
        /// Initialize the collection <see cref="ScanningDevice"/>s with the specified <paramref name="deviceConfig"/>.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the collection's <see cref="ScanningDevice"/>s</param>
        /// <param name="systemDevices">An <see cref="ISystemDevices"/> instance that provide access to the WIA driver devices</param>
        public ScanningDevices(IImagingDeviceConfig deviceConfig, ISystemDevices systemDevices)
        {
            _systemDevices = systemDevices ?? new SystemDevices();
            GetDevices(deviceConfig);
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/>, or <see langword="null"/> if no matching device can be found</returns>
        public ScanningDevice AddDevice(string deviceID)
        {
            return AddDevice(deviceID, null);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">If a device with the same <see cref="IDevice.DeviceID"/> already exists in the collection</exception>
        public void AddDevice(IDevice device)
        {
            AddDevice((ScanningDevice)device);
        }

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
        public ScanningDevice AddDevice(string deviceID, IImagingDeviceConfig config)
        {
            ScanningDevice device = null;

            // Attempt to connect a device matching the deviceID
            var settings = _systemDevices.ScannerSettings(ConnectRetries).FirstOrDefault(x => x.Id == deviceID);

            if (settings != null)
            {
                device = new ScanningDevice(settings, config);
                AddDevice(device);
            }

            return device;
        }

        public void DisableAll()
        {
            SetByEnabled(false);
        }

        public void EnableAll()
        {
            SetByEnabled(true);
        }

        /// <summary>
        /// Retrieve <see cref="ScanningDevice"/>s that are connected to the system and add them to the collection.
        /// Update the status of any devices are already present in the collection.
        /// </summary>
        public void RefreshDevices()
        {
            RefreshDevices(new ScanningDeviceSettings());
        }

        /// <inheritdoc cref="RefreshDevices()"/>
        public void RefreshDevices(IImagingDeviceConfig deviceConfig, bool enableDevices = true)
        {
            const string DEVICE_ID_KEY = "Unique Device ID";
            var deviceIds = new List<string>();

            // Check the static properties for changed IDs and only retrieve ScannerSetting instances if necessary.
            // This saves connecting the devices via the WIA driver and is much faster
            var deviceProperties = _systemDevices.ScannerProperties(connectRetries: ConnectRetries);
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
                var device = (ScanningDevice)_devices.FirstOrDefault(d => d.DeviceID == deviceId);
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
                var existingDevice = (ScanningDevice)_devices.FirstOrDefault(d => d.DeviceID == deviceId && !d.IsConnected);
                if (existingDevice is not null)
                {
                    // Remove the existing device and replace with the updated
                    bool enabled = existingDevice.IsEnabled;
                    _devices.Remove(existingDevice);

                    var device = AddDevice(deviceId, deviceConfig);
                    device.IsEnabled = enabled;
                }
            }
        }

        public void SetDeviceEnabled(string deviceID, bool enabled)
        {
            _devices.First(x => x.DeviceID == deviceID).IsEnabled = enabled;
        }

        /// <summary>
        /// A collection of <see cref="IImagingDevice"/> instances representing physical devices connected to the system.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the <see cref="ScanningDevice"/> instances</param>
        /// <returns>A collection of <see cref="ScanningDevice"/> instances representing physical scanning devices connected to the system</returns>
        private IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig)
            => ScannerDevices(deviceConfig, 1);

        /// <inheritdoc cref="ScannerDevices(IImagingDeviceConfig)"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        private IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig, int connectRetries = 1)
        {
            foreach (var settings in _systemDevices.ScannerSettings(connectRetries))
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
        private int GetDevices(IImagingDeviceConfig deviceConfig)
        {
            deviceConfig ??= new ScanningDeviceSettings();

            // Populate a new collection of scanners using specified image settings
            _devices.Clear();
            foreach (ScanningDevice device in ScannerDevices(deviceConfig, ConnectRetries))
            {
                device.IsEnabled = true;
                AddDevice(device);
            }

            return _devices.Count;
        }

        /// <summary>
        /// Set the <see cref="ScanningDevice.IsEnabled"/> property on all devices in the collection.
        /// </summary>
        /// <param name="enabled">Sets the <see cref="ScanningDevice.IsEnabled"/> property</param>
        private void SetByEnabled(bool enabled)
        {
            // Set the IsEnabled property on all members of the collection
            foreach (IImagingDevice device in _devices)
            {
                device.IsEnabled = enabled;
            }
        }
    }
}