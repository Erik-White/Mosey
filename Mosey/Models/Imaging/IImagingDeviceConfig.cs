namespace Mosey.Models.Imaging
{
    /// <summary>
    /// Device setting used by an <see cref="IImagingDevice"/> when capturing an image.
    /// </summary>
    public interface IImagingDeviceConfig
    {
        ImageColorFormat ColorFormat { get; set; }
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }
}
