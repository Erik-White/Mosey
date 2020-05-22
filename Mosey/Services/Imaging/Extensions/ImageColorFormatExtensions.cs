﻿using System;
using System.Collections.Generic;
using System.Text;
using DNTScanner.Core;
using Mosey.Models;

namespace Mosey.Services.Imaging.Extensions
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
            switch (colorFormat)
            {
                case ImageColorFormat.BlackAndWhite:
                    return ColorType.BlackAndWhite;
                case ImageColorFormat.Greyscale:
                    return ColorType.Greyscale;
                default:
                    return ColorType.Color;
            }
        }
    }
}
