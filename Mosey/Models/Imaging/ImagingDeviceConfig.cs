namespace Mosey.Models.Imaging
{
    /// <summary>
    /// Device settings used by an <see cref="IImagingDevice"/> when capturing an image.
    /// </summary>
    public record ImagingDeviceConfig(int Resolution, int Brightness, int Contrast, ImageColorFormat ColorFormat = ImageColorFormat.Color)
    {
        public ImagingDeviceConfig()
            : this(default, default, default)
        {
        }
    }
}
