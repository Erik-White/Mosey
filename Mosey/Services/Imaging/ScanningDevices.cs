using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using DNTScanner.Core;
using Mosey.Models;
using System.Collections.ObjectModel;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// A persistent collection of WIA enabled scanners
    /// Items are not removed from the collection when it is refreshed
    /// Instead, their status is updated to indicate that they may not be connected
    /// </summary>
    public class ScanningDevices : IImagingDevices<IImagingDevice>
    {
        public int ConnectRetries { get; set; } = 10;
        public IEnumerable<IImagingDevice> Devices { get { return _devices; } }
        public bool IsImagingInProgress { get { return _devices.Any(x => x.IsImaging is true); } }
        public bool IsEmpty { get { return (_devices.Count == 0); } }
        private ICollection<IImagingDevice> _devices = new ObservableItemsCollection<IImagingDevice>();
        private static System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1, 1);

        public ScanningDevices()
        {
        }

        public ScanningDevices(IImagingDeviceConfig deviceConfig)
        {
            GetDevices(deviceConfig);
        }

        public void GetDevices()
        {
            // Populate a new collection of scanners using default image settings
            GetDevices(new ScanningDeviceSettings());
        }

        public void GetDevices(IImagingDeviceConfig deviceConfig)
        {
            // Empty the collection
            _devices.Clear();

            // Populate a new collection of scanners using specified image settings
            foreach (ScanningDevice device in SystemScanners(deviceConfig, ConnectRetries))
            {
                device.IsEnabled = true;
                AddDevice(device);
            }
        }

        public void RefreshDevices()
        {
            // Get a new collection of devices if none already present
            if (_devices.Count == 0)
            {
                GetDevices();
            }
            else
            {
                RefreshDevices(new ScanningDeviceSettings());
            }
        }

        public void RefreshDevices(IImagingDeviceConfig deviceConfig, bool enableDevices = true)
        {
            IList<IDictionary<string, object>> deviceProperties = SystemScannerProperties(connectRetries: ConnectRetries);
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

        /// <summary>
        /// Add a <see cref="ScanningDevice"/> to the collection.
        /// </summary>
        /// <param name="device">A <see cref="ScanningDevice"/> instance</param>
        /// <exception cref="ArgumentException">If a device with the same <see cref="IDevice.ID"/> already exists in the collection</exception>
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
        /// Returns <see langword="null"/> if no matching device can be found
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/></returns>
        public ScanningDevice AddDevice(string deviceID)
        {
            return AddDevice(deviceID, null);
        }

        /// <summary>
        /// Attempts to create a <see cref="ScanningDevice"/> instance using the <paramref name="deviceID"/>
        /// Returns <see langword="null"/> if no matching device can be found
        /// </summary>
        /// <param name="deviceID">A unique <see cref="IDevice.DeviceID"/> to match to a device listed by the WIA driver</param>
        /// <param name="config"><see cref="IImagingDeviceConfig"/> settings for the returned <see cref="ScanningDevice"/></param>
        /// <param name="connectRetries">The number of attempts to retry connecting to the WIA driver</param>
        /// <returns>A <see cref="ScanningDevice"/> instance matching the <paramref name="deviceID"/></returns>
        public ScanningDevice AddDevice(string deviceID, IImagingDeviceConfig config, int connectRetries = 1)
        {
            _semaphore.Wait();
            ScannerSettings settings = null;

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                try
                {
                    // Attempt to create a new ScanningDevice from the deviceID
                    settings = SystemDevices.GetScannerDevices().Where(x => x.Id == deviceID).FirstOrDefault();
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            if (settings != null)
            {
                ScanningDevice device = new ScanningDevice(settings, config);
                AddDevice(device);

                return device;
            }
            else
            {
                return null;
            }
        }

        public void SetDeviceEnabled(IImagingDevice device, bool enabled)
        {
            _devices.Where(x => x.DeviceID == device.DeviceID).First().IsEnabled = enabled;
        }

        public void SetDeviceEnabled(string deviceID, bool enabled)
        {
            _devices.Where(x => x.DeviceID == deviceID).First().IsEnabled = enabled;
        }

        public void EnableAll()
        {
            SetByEnabled(true);
        }

        public void DisableAll()
        {
            SetByEnabled(false);
        }

        private void SetByEnabled(bool enabled)
        {
            // Set the IsEnabled property on all members of the collection
            foreach (IImagingDevice device in _devices)
            {
                device.IsEnabled = enabled;
            }
        }

        public IEnumerable<IImagingDevice> GetByEnabled(bool enabled)
        {
            return _devices.Where(x => x.IsEnabled = enabled).AsEnumerable();
        }

        /// <summary>
        /// Lists the static properties of scanners connected to the system.
        /// </summary>
        private IList<IDictionary<string, object>> SystemScannerProperties(int connectRetries = 1)
        {
            _semaphore.Wait();
            IList<IDictionary<string, object>> properties = new List<IDictionary<string, object>>();

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                try
                {
                    properties = SystemDevices.GetScannerDeviceProperties();
                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return properties;
        }

        private IEnumerable<IImagingDevice> SystemScanners(IImagingDeviceConfig deviceConfig, int connectRetries = 1)
        {
            List<IImagingDevice> deviceList = new List<IImagingDevice>();

            // Wait until the WIA device manager is ready
            while (connectRetries > 0)
            {
                _semaphore.Wait();

                try
                {
                    var systemScanners = SystemDevices.GetScannerDevices();

                    // Check that at least one scanner can be found
                    if (systemScanners.FirstOrDefault() == null)
                    {
                        return deviceList;
                    }

                    foreach (ScannerSettings settings in systemScanners)
                    {
                        // Store the device in the collection
                        deviceList.Add(new ScanningDevice(settings, deviceConfig));
                    }

                    break;
                }
                catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                {
                    if (--connectRetries > 0)
                    {
                        // Wait until the scanner is ready if it is warming up, busy etc
                        // Also retry if the WIA driver does not respond in time (NullReference)
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    throw;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return deviceList;
        }
    }
}