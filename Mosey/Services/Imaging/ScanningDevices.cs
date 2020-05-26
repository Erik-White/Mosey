using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
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
        /// <summary>
        /// The number of time to attempt reconnection to the WIA driver.
        /// </summary>
        public int ConnectRetries { get; set; } = 5;

        /// <summary>
        /// A collection of <see cref="ScanningDevice"/>s, representing physical scanners.
        /// </summary>
        public IEnumerable<IImagingDevice> Devices { get { return _devices; } }

        public bool IsEmpty { get { return (_devices.Count == 0); } }
        public bool IsImagingInProgress { get { return _devices.Any(x => x.IsImaging is true); } }
        private ICollection<IImagingDevice> _devices = new ObservableItemsCollection<IImagingDevice>();

        /// <summary>
        /// Initialize an empty collection.
        /// </summary>
        public ScanningDevices()
        {
        }

        /// <summary>
        /// Initialize the collection <see cref="ScanningDevice"/>s with the specified <paramref name="deviceConfig"/>.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the collection's <see cref="ScanningDevice"/>s</param>
        public ScanningDevices(IImagingDeviceConfig deviceConfig)
        {
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
                _devices.Add((IImagingDevice)device);
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
            var settings = SystemDevices.ScannerSettings(ConnectRetries).Where(x => x.Id == deviceID).FirstOrDefault();

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

        public IEnumerable<IImagingDevice> GetByEnabled(bool enabled)
        {
            return _devices.Where(x => x.IsEnabled == enabled).AsEnumerable();
        }

        /// <summary>
        /// Retrieve <see cref="ScanningDevice"/>s that are connected to the system and add them to the collection.
        /// Update the status of any devices are already present in the collection.
        /// </summary>
        public void RefreshDevices()
        {
            // Get a new collection of devices if none already present
            if (IsEmpty)
            {
                GetDevices();
            }
            else
            {
                RefreshDevices(new ScanningDeviceSettings());
            }
        }

        /// <inheritdoc cref="RefreshDevices(IImagingDeviceConfig, bool)"/>
        public void RefreshDevices(IImagingDeviceConfig deviceConfig, bool enableDevices = true)
        {
            IList<IDictionary<string, object>> deviceProperties = SystemDevices.ScannerProperties(connectRetries: ConnectRetries);
            if (deviceProperties.Count == 0)
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
            foreach (IDictionary<string, object> properties in deviceProperties)
            {
                string deviceID = properties["Unique Device ID"].ToString();

                if (!_devices.Where(d => d.DeviceID == deviceID).Any())
                {
                    // Create a new device and add it to the collection
                    ScanningDevice device = AddDevice(deviceID, deviceConfig);
                    device.IsEnabled = enableDevices;
                }
                else
                {
                    ScanningDevice existingDevice = (ScanningDevice)_devices.Where(d => d.DeviceID == deviceID && !d.IsConnected).FirstOrDefault();
                    if (existingDevice != null)
                    {
                        // Remove the existing device from the collection
                        bool enabled = existingDevice.IsEnabled;
                        _devices.Remove(existingDevice);

                        // Replace with the new and updated device
                        ScanningDevice device = AddDevice(deviceID, deviceConfig);
                        device.IsEnabled = enabled;
                    }
                }

                // If the device is in the collection but no longer found
                IEnumerable<IImagingDevice> devicesRemoved = _devices.Where(l1 => !deviceProperties.Any(l2 => l1.DeviceID == l2["Unique Device ID"].ToString()));
                if (devicesRemoved.Count() > 0)
                {
                    foreach (ScanningDevice device in devicesRemoved)
                    {
                        device.IsConnected = false;
                    }
                }
            }
        }

        public void SetDeviceEnabled(string deviceID, bool enabled)
        {
            _devices.Where(x => x.DeviceID == deviceID).First().IsEnabled = enabled;
        }

        public void SetDeviceEnabled(IImagingDevice device, bool enabled)
        {
            _devices.Where(x => x.DeviceID == device.DeviceID).First().IsEnabled = enabled;
        }

        /// <summary>
        /// Retrieve <see cref="ScanningDevice"/>s that are connected to the system and add them to the collection.
        /// The exisiting items in the collection are cleared first.
        /// </summary>
        /// <returns>The number of devices added to the collection</returns>
        private int GetDevices()
        {
            // Populate a new collection of scanners using default image settings
            return GetDevices(new ScanningDeviceSettings());
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
            // Empty the collection
            _devices.Clear();

            // Populate a new collection of scanners using specified image settings
            foreach (ScanningDevice device in SystemDevices.ScannerDevices(deviceConfig, ConnectRetries))
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