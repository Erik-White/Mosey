using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mosey.Models
{
    /// <summary>
    /// A collection of <see cref="IImagingDevice"/>s representing physical imaging devices.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IImagingDevices<T> : IDeviceCollection<T> where T : IImagingDevice
    {
        /// <summary>
        /// <see langword="true"/> if the <see cref="IDeviceCollection{T}.Devices"/> collection is empty
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// <see langword="true"/> if any device in the collection has the <see cref="IImagingDevice.IsImaging"/> property of <see langword="true"/>.
        /// </summary>
        bool IsImagingInProgress { get; }

        /// <summary>
        /// Retrieve <see cref="IImagingDevice"/>s that are connected to the system and add them to the collection.
        /// Update the status of any devices are already present in the collection.
        /// </summary>
        /// <param name="deviceConfig">The <see cref="IImagingDeviceConfig"/> used to initialize the device</param>
        /// <param name="enableDevices">Sets the <see cref="IDevice.IsEnabled"/> property of any new devices that are not already in the collection</param>
        void RefreshDevices(IImagingDeviceConfig deviceConfig, bool enableDevices);
    }

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

    /// <summary>
    /// Device setting used by an <see cref="IImagingDevice"/> when capturing an image.
    /// </summary>
    public interface IImagingDeviceConfig : IConfig
    {
        ImageColorFormat ColorFormat { get; set; }
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }

    /// <summary>
    /// Image settings used when writing an image captured by an <see cref="IImagingDevice"/> to disk.
    /// </summary>
    public interface IImageFileConfig : IConfig
    {
        /// <summary>
        /// The directory used to store the image.
        /// </summary>
        string Directory { get; set; }

        /// <summary>
        /// The image file prefix.
        /// </summary>
        string Prefix { get; set; }

        /// <summary>
        /// The image format file extension <see cref="string"/> used to store the image.
        /// </summary>
        string Format { get; set; }

        /// <summary>
        /// A list of allowed <see cref="Format"/> strings.
        /// </summary>
        List<string> SupportedFormats { get; set; }

        /// <summary>
        /// The format used for image file timestamp information.
        /// </summary>
        string DateFormat { get; set; }

        /// <summary>
        /// The format used for image file timestamp information.
        /// </summary>
        string TimeFormat { get; set; }
    }
}
