using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DNTScanner.Core;
using Mosey.Models.Imaging;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Provides access to the WIA driver devices via DNTScanner.Core
    /// </summary>
    internal sealed class SystemDevices : ISystemImagingDevices<ScannerSettings>
    {
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public int ConnectRetries { get; set; } = 5;

        public TimeSpan ConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <inheritdoc/>
        /// <exception cref="COMException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        public IEnumerable<byte[]> PerformImaging(ScannerSettings settings, IImagingDeviceConfig config, IImagingDevice.ImageFormat format)
        {
            IEnumerable<byte[]> images = new List<byte[]>();

            // Connect to the specified device and create a COM object representation
            using (var device = ConfiguredScannerDevice(settings, config))
            {
                WIARetry(() => device.PerformScan(
                    format.ToWIAImageFormat()),
                    ConnectRetries,
                    ConnectRetryDelay,
                    _semaphore);
                images = WIARetry(device.ExtractScannedImageFiles, ConnectRetries, ConnectRetryDelay, _semaphore).ToList();
            }

            return images;
        }

        /// <inheritdoc/>
        /// <exception cref="COMException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        public IList<IDictionary<string, object>> GetDeviceProperties()
        {
            return WIARetry(
                    DNTScanner.Core.SystemDevices.GetScannerDeviceProperties,
                    ConnectRetries,
                    ConnectRetryDelay,
                    _semaphore);
        }

        /// <inheritdoc/>
        /// <exception cref="COMException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <see cref="ConnectRetries"/></exception>
        public IEnumerable<ScannerSettings> GetDeviceSettings()
        {
            return WIARetry(
                DNTScanner.Core.SystemDevices.GetScannerDevices,
                ConnectRetries,
                ConnectRetryDelay,
                _semaphore).AsEnumerable();
        }

        /// <summary>
        /// Create and configure a new <see cref="ScannerDevice"/> instance.
        /// </summary>
        /// <param name="settings">A <see cref="GetScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <returns>A <see cref="ScannerDevice"/> instance configured using <paramref name="config"/></returns>
        private ScannerDevice ConfiguredScannerDevice(ScannerSettings settings, IImagingDeviceConfig config)
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

        /// <inheritdoc cref="WIARetry{T}(Func{T}, int, TimeSpan, SemaphoreSlim)"/>
        private static void WIARetry(Action method, int connectRetries, TimeSpan retryDelay, SemaphoreSlim semaphore = null)
        {
            // Wrap the Action in a Func with a dummy return value
            Func<bool> methodFunc = () => { method(); return true; };
            WIARetry(methodFunc, connectRetries, retryDelay, semaphore);
        }

        /// <summary>
        /// Provides a try-retry wrapper for functions that need to access the WIA driver
        /// </summary>
        /// <remarks>
        /// The WIA driver will produce COMExceptions if attempting to connect to a device that is
        /// not ready, cannot be found etc. In many cases a successful connection can be made by reattempting
        /// shortly after.
        /// <para/>
        /// The DNTScanner wrapper does not provide any events or async methods that would allow for simply
        /// awaiting the WIA driver directly
        /// </remarks>
        /// <typeparam name="T">The return type of <paramref name="method"/></typeparam>
        /// <param name="method">The function that requires access to the WIA driver</param>
        /// <param name="connectRetries">The number of attempts to attempt connecting to the WIA driver, after <paramref name="retryDelay"/></param>
        /// <param name="semaphore">Coordinates access to the WIA driver</param>
        /// <param name="retryDelay">The time between <paramref name="connectRetries"/> attempts</param>
        /// <returns></returns>
        /// <exception cref="COMException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        /// <exception cref="NullReferenceException">If an error occurs within the specified number of <paramref name="connectRetries"/></exception>
        private static T WIARetry<T>(Func<T> method, int connectRetries, TimeSpan retryDelay, SemaphoreSlim semaphore = null)
        {
            T result = default;
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
                            Thread.Sleep(retryDelay);
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
