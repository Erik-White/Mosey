using System;
using DNTScanner.Core;
using Mosey.Models.Imaging;

namespace Mosey.Core.Services.Imaging.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ImageColorFormat"/>
    /// </summary>
    public static class ImageColorFormatExtensions
    {
        /// <summary>
        /// Returns a <see cref="ColorType"/> from a <see cref="ColorFormat"/> <see cref="Enum"/>.
        /// </summary>
        /// <param name="colorFormat"></param>
        /// <returns>A matching <see cref="ColorType"/>, or <see cref="ColorType.Color"/> if no match can be found</returns>
        public static ColorType ToColorType(this ImageColorFormat colorFormat)
        {
            // ColorType properties are internal and not accessible for comparison
            return colorFormat switch
            {
                ImageColorFormat.BlackAndWhite => ColorType.BlackAndWhite,
                ImageColorFormat.Greyscale => ColorType.Greyscale,
                _ => ColorType.Color,
            };
        }
    }
}
