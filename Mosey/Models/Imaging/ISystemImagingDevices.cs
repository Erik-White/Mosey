using System.Collections.Generic;

namespace Mosey.Models.Imaging
{
    /// <summary>
    /// Provides access imaging devices connected to the system
    /// </summary>
    public interface ISystemImagingDevices<T>
    {
        /// <summary>
        /// Retrieve image(s) from a scanner.
        /// </summary>
        /// <param name="settings">A <see cref="T"/> instance representing a physical device</param>
        /// <param name="config">Device settings used when capturing an image</param>
        /// <param name="format">The image format used internally for storing the image</param>
        /// <returns>A list of retrieved images as byte arrays, in <paramref name="format"/></returns>
        IEnumerable<byte[]> PerformImaging(T settings, ImagingDeviceConfig config, IImagingDevice.ImageFormat format);

        /// <summary>
        /// Lists the static properties of imaging devices connected to the system.
        /// </summary>
        /// <remarks>
        /// Static device properties are limited, but can be retrieved without establishing a connection to the device.
        /// </remarks>
        /// <returns>A list of the static device properties</returns>
        IList<IDictionary<string, object>> GetDeviceProperties();

        /// <summary>
        /// A collection of <see cref="T"/> representing physical devices connected to the system.
        /// </summary>
        /// <returns>A collection of <see cref="T"/> representing physical devices connected to the system.</returns>
        IEnumerable<T> GetDeviceSettings();
    }
}
