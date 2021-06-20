using System;
using System.Drawing.Imaging;

namespace Mosey.Services.Imaging.Extensions
{
    public static class ImageFormatExtensions
    {
        /// <summary>
        /// Convert a file extension string to an <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/>.
        /// </summary>
        /// <param name="value">An <see cref="ScanningDevice.ImageFormat"/> instance</param>
        /// <param name="imageFormatStr">A file extension <see cref="string"/></param>
        /// <returns>An <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/></returns>
        public static ScanningDevice.ImageFormat FromString(this ScanningDevice.ImageFormat _, string imageFormatStr)
        {
            return Enum.TryParse(imageFormatStr, ignoreCase: true, out ScanningDevice.ImageFormat value)
                ? value
                : throw new ArgumentException($"{imageFormatStr} is not a supported image file extension.");
        }

        /// <summary>
        /// Convert to a <see cref="ImageFormat"/> instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A <see cref="ImageFormat"/> instance</returns>
        public static ImageFormat ToDrawingImageFormat(this ScanningDevice.ImageFormat value)
        {
            return (ImageFormat)typeof(ImageFormat)
                    .GetProperty(value.ToString(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase)
                    .GetValue(null);
        }

        /// <summary>
        /// Convert to a <see cref="DNTScanner.Core.WiaImageFormat"/> instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A <see cref="DNTScanner.Core.WiaImageFormat"/> instance</returns>
        public static DNTScanner.Core.WiaImageFormat ToWIAImageFormat(this ScanningDevice.ImageFormat value)
        {
            return (DNTScanner.Core.WiaImageFormat)typeof(DNTScanner.Core.WiaImageFormat)
                    .GetProperty(value.ToString(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase)
                    .GetValue(null);
        }
    }
}
