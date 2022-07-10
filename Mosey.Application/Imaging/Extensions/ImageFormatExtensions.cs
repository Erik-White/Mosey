using System;
using Mosey.Core.Imaging;

namespace Mosey.Application.Imaging.Extensions
{
    public static class ImageFormatExtensions
    {
        /// <summary>
        /// Convert a file extension string to an <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/>.
        /// </summary>
        /// <param name="value">An <see cref="ScanningDevice.ImageFormat"/> instance</param>
        /// <param name="imageFormatStr">A file extension <see cref="string"/></param>
        /// <returns>An <see cref="ScanningDevice.ImageFormat"/> <see cref="Enum"/></returns>
        public static IImagingDevice.ImageFormat FromString(this IImagingDevice.ImageFormat _, string imageFormatStr)
            => Enum.TryParse(imageFormatStr, ignoreCase: true, out IImagingDevice.ImageFormat value)
                ? value
                : throw new ArgumentException($"{imageFormatStr} is not a supported image file extension.");

        /// <summary>
        /// Convert to a <see cref="DNTScanner.Core.WiaImageFormat"/> instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A <see cref="DNTScanner.Core.WiaImageFormat"/> instance</returns>
        public static DNTScanner.Core.WiaImageFormat ToWIAImageFormat(this IImagingDevice.ImageFormat value)
            => (DNTScanner.Core.WiaImageFormat)typeof(DNTScanner.Core.WiaImageFormat)
                .GetProperty(value.ToString(), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.IgnoreCase)
                .GetValue(null);
    }
}
