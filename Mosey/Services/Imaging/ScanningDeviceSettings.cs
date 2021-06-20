using System;
using DNTScanner.Core;
using Mosey.Models.Imaging;

namespace Mosey.Services.Imaging
{
    /// <summary>
    /// Device settings used by a <see cref="ScanningDevice"/> when capturing an image.
    /// </summary>
    public class ScanningDeviceSettings : IImagingDeviceConfig, IEquatable<ScanningDeviceSettings>
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
            => MemberwiseClone();

        public override bool Equals(object obj)
            => Equals(obj as ScanningDeviceSettings);

        public bool Equals(ScanningDeviceSettings other)
        {
            return other is not null &&
                (ColorFormat, Resolution, Brightness, Contrast).Equals(
                    (other.ColorFormat, other.Resolution, other.Brightness, other.Contrast));
        }

        public override int GetHashCode()
            => (ColorFormat, Resolution, Brightness, Contrast).GetHashCode();
    }
}
