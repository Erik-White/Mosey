using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mosey.Models.Imaging
{
    /// <summary>
    /// Represents a physical imaging device connected to the system.
    /// </summary>
    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged, IDevice
    {
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
        IImagingDeviceConfig ImageSettings { get; set; }

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
        void GetImage();

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// </summary>
        void SaveImage();

        /// <summary>
        /// Write an image captured with <see cref="GetImage"/> to disk
        /// </summary>
        /// <param name="fileName">The image file name, the file extension ignored and instead inferred from <paramref name="fileFormat"/></param>
        /// <param name="directory">The directory path to use when storing the image</param>
        /// <param name="fileFormat">The image format file extension <see cref="string"/> used to store the image</param>
        /// <returns>A collection of file path <see cref="string"/>s for the newly created images</returns>
        IEnumerable<string> SaveImage(string fileName, string directory, string fileFormat);
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
