using System;
using System.Collections.Generic;
using System.Text;
using DNTScanner.Core;
using Mosey.Models;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Device settings used by a <see cref="ScanningDevice"/> when capturing an image.
    /// </summary>
    public class ScanningDeviceSettings : IImagingDeviceConfig
    {
        public ImageColorFormat ColorFormat { get; set; } = ImageColorFormat.Color;
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }

        /// <summary>
        /// Initialize a new <see cref="ScannerDeviceSettings"/> instance.
        /// </summary>
        public ScanningDeviceSettings() { }

        /// <summary>
        /// Initialize a new <see cref="ScannerDeviceSettings"/> instance.
        /// </summary>
        /// <param name="colorFormat">Image <see cref="ImageColorFormat"/></param>
        /// <param name="resolution">Image resolution</param>
        /// <param name="brightness">Image brightness</param>
        /// <param name="contrast">Image contrast</param>
        public ScanningDeviceSettings(ImageColorFormat colorFormat, int resolution, int brightness, int contrast)
        {
            ColorFormat = colorFormat;
            Resolution = resolution;
            Brightness = brightness;
            Contrast = contrast;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
