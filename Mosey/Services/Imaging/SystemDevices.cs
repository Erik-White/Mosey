using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using DNTScanner.Core;
using Mosey.Models;

namespace Mosey.Services.Imaging
{
    internal static class SystemDevices
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// A collection of <see cref="IImagingDevice"/> instances representing physical devices connected to the system.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the <see cref="ScanningDevice"/> instances</param>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <param name="semaphore">A semaphore to coordinate connections to the WIA driver</param>
        /// <returns>A collection of <see cref="ScanningDevice"/> instances representing physical scanning devices connected to the system</returns>
        internal static IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig, int connectRetries = 1, SemaphoreSlim semaphore = null)
        {
            var devices = new List<IImagingDevice>();

            foreach (var settings in ScannerSettings(connectRetries, semaphore))
            {
                // Store the device in the collection
                devices.Add(new ScanningDevice(settings, deviceConfig));
            }

            return devices;
        }

        /// <summary>
        /// Lists the static properties of scanners connected to the system.
        /// Use the <see cref="ScannerDevices"/> function to retrieve full device instances.
        /// </summary>
        /// <remarks>
        /// Static device properties are limited, but can be retrieved without establishing a connection to the device.
        /// </remarks>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <param name="semaphore">A semaphore to coordinate connections to the WIA driver</param>
        /// <returns>A list of the static device properties</returns>
        internal static IList<IDictionary<string, object>> ScannerProperties(int connectRetries = 1, SemaphoreSlim semaphore = null)
        {
            IList<IDictionary<string, object>> properties = new List<IDictionary<string, object>>();
            semaphore ??= _semaphore;
            semaphore.Wait();

            try
            {
                // Wait until the WIA device manager is ready
                while (connectRetries > 0)
                {
                    try
                    {
                        properties = DNTScanner.Core.SystemDevices.GetScannerDeviceProperties();
                        break;
                    }
                    catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                    {
                        if (--connectRetries > 0)
                        {
                            // Wait until the scanner is ready if it is warming up, busy etc
                            // Also retry if the WIA driver does not respond in time (NullReference)
                            Thread.Sleep(1000);
                            continue;
                        }
                        throw;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            return properties;
        }

        /// <summary>
        /// A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.
        /// </summary>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <param name="semaphore">A semaphore to coordinate connections to the WIA driver</param>
        /// <returns>A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.</returns>
        internal static IEnumerable<ScannerSettings> ScannerSettings(int connectRetries = 1, SemaphoreSlim semaphore = null)
        {
            var deviceList = new List<ScannerSettings>();
            semaphore ??= _semaphore;
            semaphore.Wait();

            try
            {
                // Wait until the WIA device manager is ready
                while (connectRetries > 0)
                {
                    try
                    {
                        var systemScanners = DNTScanner.Core.SystemDevices.GetScannerDevices();

                        // Check that at least one scanner can be found
                        if (systemScanners.FirstOrDefault() == null)
                        {
                            return deviceList;
                        }

                        foreach (ScannerSettings settings in systemScanners)
                        {
                            // Store the device in the collection
                            deviceList.Add(settings);
                        }

                        break;
                    }
                    catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                    {
                        if (--connectRetries > 0)
                        {
                            // Wait until the scanner is ready if it is warming up, busy etc
                            // Also retry if the WIA driver does not respond in time (NullReference)
                            Thread.Sleep(1000);
                            continue;
                        }
                        throw;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            return deviceList;
        }
    }
}
