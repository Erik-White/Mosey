using System.Collections.Generic;

namespace Mosey.Models.Imaging
{
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
