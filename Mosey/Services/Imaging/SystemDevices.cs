﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using DNTScanner.Core;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Provides access to the WIA driver devices via DNTScanner.Core
    /// </summary>
    internal static class SystemDevices
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Retrieve image(s) from a scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="format">The image format used internally for storing the image</param>
        /// <param name="connectRetries">The number of attempts to try connecting to the WIA driver, after <paramref name="delay"/></param>
        /// <param name="delay">The time in millseconds between <paramref name="connectRetries"/> attempts</param>
        /// <returns>A list of retrieved images as byte arrays, in <paramref name="format"/></returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        internal static IEnumerable<byte[]> PerformScan(ScannerSettings settings, IImagingDeviceConfig config, ScanningDevice.ImageFormat format, int connectRetries = 1, int delay = 1000)
        {
            IEnumerable<byte[]> images = new List<byte[]>();

            // Connect to the specified device and create a COM object representation
            using (var device = ConfiguredScannerDevice(settings, config))
            {
                WIARetry(() => { device.PerformScan(format.ToWIAImageFormat()); }, connectRetries, _semaphore, delay);
                images = WIARetry(device.ExtractScannedImageFiles, connectRetries, _semaphore, delay).ToList();
            }

            return images;
        }

        /// <summary>
        /// A collection of <see cref="IImagingDevice"/> instances representing physical devices connected to the system.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the <see cref="ScanningDevice"/> instances</param>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <param name="semaphore">A semaphore to coordinate connections to the WIA driver</param>
        /// <returns>A collection of <see cref="ScanningDevice"/> instances representing physical scanning devices connected to the system</returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        internal static IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig, int connectRetries = 1)
        {
            var devices = new List<IImagingDevice>();

            foreach (var settings in ScannerSettings(connectRetries))
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
        /// <returns>A list of the static device properties</returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        internal static IList<IDictionary<string, object>> ScannerProperties(int connectRetries = 1)
        {
            IList<IDictionary<string, object>> properties = new List<IDictionary<string, object>>();

            properties = WIARetry(
                    DNTScanner.Core.SystemDevices.GetScannerDeviceProperties,
                    connectRetries,
                    _semaphore
                    );

            return properties;
        }

        /// <summary>
        /// A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.
        /// </summary>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <returns>A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.</returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        internal static IEnumerable<ScannerSettings> ScannerSettings(int connectRetries = 1)
        {
            var deviceList = new List<ScannerSettings>();

            var systemScanners = WIARetry(
                DNTScanner.Core.SystemDevices.GetScannerDevices,
                connectRetries,
                _semaphore
                );

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

            return deviceList;
        }

        /// <summary>
        /// Create and configure a new <see cref="ScannerDevice"/> instance.
        /// </summary>
        /// <remarks>
        /// If the resolution specified in <paramref name="config"/> is not available,
        /// the closest value in <see cref="ScannerSettings.SupportedResolutions"/> will be used.
        /// </remarks>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <returns>A <see cref="ScannerDevice"/> instance configured using <paramref name="config"/></returns>
        private static ScannerDevice ConfiguredScannerDevice(ScannerSettings settings, IImagingDeviceConfig config)
        {
            var device = new ScannerDevice(settings);
            var supportedResolutions = settings.SupportedResolutions;

            // Check that the selected resolution is supported by this device
            if (!supportedResolutions.Contains(config.Resolution))
            {
                // Find the closest supported resolution instead
                config.Resolution = supportedResolutions
                    .OrderBy(v => v)
                    .OrderBy(item => Math.Abs(config.Resolution - item))
                    .First();
            }

            // Apply device config
            device.ScannerPictureSettings(pictureConfig =>
                pictureConfig
                    .ColorFormat(config.ColorFormat.ToColorType())
                    .Resolution(config.Resolution)
                    .Brightness(config.Brightness)
                    .Contrast(config.Contrast)
                    .StartPosition(left: 0, top: 0)
                );

            return device;
        }

        /// <summary>
        /// Provides a try-retry wrapper for functions that need to access the WIA driver
        /// </summary>
        /// <remarks>
        /// Wraps <paramref name="method"/> in a <see cref="Func<>"/> to pass through to <see cref="WIARetry{T}(Func{T}, int, SemaphoreSlim, int)/>.
        /// </remarks>
        /// <param name="method">The function that requires access to the WIA driver</param>
        /// <param name="connectRetries">The number of attempts to try connecting to the WIA driver, after <paramref name="delay"/></param>
        /// <param name="semaphore">Coordinates access to the WIA driver</param>
        /// <param name="delay">The time in millseconds between <paramref name="connectRetries"/> attempts</param>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        private static void WIARetry(Action method, int connectRetries = 1, SemaphoreSlim semaphore = null, int delay = 1000)
        {
            // Wrap the Action in a Func with a dummy return value
            Func<bool> methodFunc = () => { method(); return true; };
            WIARetry(methodFunc, connectRetries, semaphore, delay);
        }

        /// <summary>
        /// Provides a try-retry wrapper for functions that need to access the WIA driver
        /// </summary>
        /// <remarks>
        /// The WIA driver will produce COMExceptions if attempting to connect to a device that is
        /// not ready, cannot be found etc. In many cases a successful connection can be made by reattempting
        /// shortly after.
        /// The DNTScanner wrapper does not provide any events or async methods that would allow for simply
        /// awaiting the WIA driver directly
        /// </remarks>
        /// <typeparam name="T">The return type of <paramref name="method"/></typeparam>
        /// <param name="method">The function that requires access to the WIA driver</param>
        /// <param name="connectRetries">The number of attempts to try connecting to the WIA driver, after <paramref name="delay"/></param>
        /// <param name="semaphore">Coordinates access to the WIA driver</param>
        /// <param name="delay">The time in millseconds between <paramref name="connectRetries"/> attempts</param>
        /// <returns></returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        private static T WIARetry<T>(Func<T> method, int connectRetries = 1, SemaphoreSlim semaphore = null, int delay = 1000)
        {
            var result = default(T);
            semaphore?.Wait();

            try
            {
                // Wait until the WIA device manager is ready
                while (connectRetries > 0)
                {
                    try
                    {
                        result = method.Invoke();
                        break;
                    }
                    catch (Exception ex) when (ex is COMException | ex is NullReferenceException)
                    {
                        if (--connectRetries > 0)
                        {
                            // Wait until the scanner is ready if it is warming up, busy etc
                            // Also retry if the WIA driver does not respond in time (NullReference)
                            Thread.Sleep(delay);
                            continue;
                        }
                        throw;
                    }
                }
            }
            finally
            {
                semaphore?.Release();
            }

            return result;
        }
    }
}