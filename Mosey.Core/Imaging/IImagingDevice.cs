using System.ComponentModel;

namespace Mosey.Core.Imaging
{
    /// <summary>
    /// Represents a physical imaging device connected to the system.
    /// </summary>
    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged, IDevice
    {
        /// <summary>
        /// The image format used for encoding images captured by the <see cref="IImagingDevice"/>.
        /// </summary>
        public enum ImageFormat
        {
            Bmp,
            Png,
            Gif,
            Jpeg,
            Tiff
        }

        /// <summary>
        /// A collection of properties detailing the device's abilities.
        /// </summary>
        IList<KeyValuePair<string, object>> DeviceSettings { get; }

        /// <summary>
        /// A collection of image byte arrays retrieved from the device.
        /// </summary>
        IList<byte[]> Images { get; }

        /// <summary>
        /// Set the device settings used when imaging.
        /// </summary>
        ImagingDeviceConfig ImageSettings { get; set; }

        /// <summary>
        /// <see langword="true"/> when imaging is in progress.
        /// </summary>
        bool IsImaging { get; }

        /// <summary>
        /// Image resolutions supported for imaging.
        /// </summary>
        IList<int> SupportedResolutions { get; }

        /// <summary>
        /// Remove all images stored in <see cref="Images"/>.
        /// </summary>
        void ClearImages();

        /// <summary>
        /// Retrieve an image from the physical imaging device.
        /// </summary>
        void PerformImaging();
    }

    /// <summary>
    /// The colour mode of images captured by an <see cref="IImagingDevice"/>.
    /// </summary>
    public enum ImageColorFormat
    {
        Color,
        BlackAndWhite,
        Greyscale
    }
}
