using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using DNTScanner.Core;
using Mosey.Models;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Provides access to the WIA driver devices via DNTScanner.Core
    /// </summary>
    public interface ISystemDevices
    {
        /// <summary>
        /// Retrieve image(s) from a scanner.
        /// </summary>
        /// <param name="settings">A <see cref="ScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="format">The image format used internally for storing the image</param>
        /// <returns>A list of retrieved images as byte arrays, in <paramref name="format"/></returns>
        IEnumerable<byte[]> PerformScan(ScannerSettings settings, IImagingDeviceConfig config, ScanningDevice.ImageFormat format);

        /// <inheritdoc cref="PerformScan(ScannerSettings, IImagingDeviceConfig, ScanningDevice.ImageFormat)"/>
        /// <param name="connectRetries">The number of attempts to try connecting to the WIA driver, after <paramref name="delay"/></param>
        /// <param name="delay">The time in millseconds between <paramref name="connectRetries"/> attempts</param>
        IEnumerable<byte[]> PerformScan(ScannerSettings settings, IImagingDeviceConfig config, ScanningDevice.ImageFormat format, int connectRetries, int delay);

        /// <summary>
        /// A collection of <see cref="IImagingDevice"/> instances representing physical devices connected to the system.
        /// </summary>
        /// <param name="deviceConfig">Used to initialize the <see cref="ScanningDevice"/> instances</param>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        /// <param name="semaphore">A semaphore to coordinate connections to the WIA driver</param>
        /// <returns>A collection of <see cref="ScanningDevice"/> instances representing physical scanning devices connected to the system</returns>
        public IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig);

        /// <inheritdoc cref="ScannerDevices(IImagingDeviceConfig)"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        public IEnumerable<IImagingDevice> ScannerDevices(IImagingDeviceConfig deviceConfig, int connectRetries);

        /// <summary>
        /// Lists the static properties of scanners connected to the system.
        /// <para/>
        /// Use the <see cref="ScannerDevices"/> function to retrieve full device instances.
        /// </summary>
        /// <remarks>
        /// Static device properties are limited, but can be retrieved without establishing a connection to the device.
        /// </remarks>
        /// <returns>A list of the static device properties</returns>
        public IList<IDictionary<string, object>> ScannerProperties();

        /// <inheritdoc cref="ScannerProperties"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        public IList<IDictionary<string, object>> ScannerProperties(int connectRetries);

        /// <summary>
        /// A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.
        /// </summary>
        /// <returns>A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.</returns>
        public IEnumerable<ScannerSettings> ScannerSettings();

        /// <inheritdoc cref="ScannerSettings"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        public IEnumerable<ScannerSettings> ScannerSettings(int connectRetries);
    }
}
