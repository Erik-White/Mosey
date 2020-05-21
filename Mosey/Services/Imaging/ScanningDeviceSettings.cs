using System;
using System.Collections.Generic;
using System.Text;
using DNTScanner.Core;
using Mosey.Models;

namespace Mosey.Services.Imaging
{
    public class ScanningDeviceSettings : IImagingDeviceConfig
    {
        public ImageColorFormat ColorFormat { get; set; } = ImageColorFormat.Color;
        public ColorType ColorType { get { return ColorTypeFromFormat(ColorFormat); } }
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }

        public ScanningDeviceSettings() { }

        public ScanningDeviceSettings(ImageColorFormat colorFormat, int resolution, int brightness, int contrast)
        {
            ColorFormat = colorFormat;
            Resolution = resolution;
            Brightness = brightness;
            Contrast = contrast;
        }

        /// <summary>
        /// Returns a ColorType class from a ColorFormat Enum.
        /// Allows conversion of type from json settings file
        /// </summary>
        /// <param name="colorFormat"></param>
        /// <returns></returns>
        public static ColorType ColorTypeFromFormat(ImageColorFormat colorFormat)
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

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
