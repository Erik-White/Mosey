using System.Collections.Generic;
using DNTScanner.Core;
using Mosey.Models.Imaging;

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
        /// <param name="settings">A <see cref="GetScannerSettings"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="format">The image format used internally for storing the image</param>
        /// <returns>A list of retrieved images as byte arrays, in <paramref name="format"/></returns>
        IEnumerable<byte[]> PerformScan(ScannerSettings settings, IImagingDeviceConfig config, ScanningDevice.ImageFormat format);

        /// <summary>
        /// Lists the static properties of scanners connected to the system.
        /// <para/>
        /// Use the <see cref="ScannerDevices"/> function to retrieve full device instances.
        /// </summary>
        /// <remarks>
        /// Static device properties are limited, but can be retrieved without establishing a connection to the device.
        /// </remarks>
        /// <returns>A list of the static device properties</returns>
        IList<IDictionary<string, object>> GetScannerProperties();

        /// <summary>
        /// A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.
        /// </summary>
        /// <returns>A collection of <see cref="ScannerSettings"/> representing physical devices connected to the system.</returns>
        IEnumerable<ScannerSettings> GetScannerSettings();
    }
}
