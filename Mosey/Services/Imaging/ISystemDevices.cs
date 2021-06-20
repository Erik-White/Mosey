using System.Collections.Generic;
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
        /// Lists the static properties of scanners connected to the system.
        /// <para/>
        /// Use the <see cref="ScannerDevices"/> function to retrieve full device instances.
        /// </summary>
        /// <remarks>
        /// Static device properties are limited, but can be retrieved without establishing a connection to the device.
        /// </remarks>
        /// <returns>A list of the static device properties</returns>
        IList<IDictionary<string, object>> ScannerProperties();

        /// <inheritdoc cref="ScannerProperties"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        IList<IDictionary<string, object>> ScannerProperties(int connectRetries);

        /// <summary>
        /// A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.
        /// </summary>
        /// <returns>A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.</returns>
        IEnumerable<ScannerSettings> ScannerSettings();

        /// <inheritdoc cref="ScannerSettings"/>
        /// <param name="connectRetries">The number of retry attempts allowed if connecting to the WIA driver was unsuccessful</param>
        IEnumerable<ScannerSettings> ScannerSettings(int connectRetries);
    }
}
