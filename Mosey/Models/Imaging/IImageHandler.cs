using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Models.Imaging
{
    public interface IImageHandler<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// Apply default encoder settings
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        void ApplyEncoderDefaults(IImageEncoder encoder);

        /// <summary>
        /// Convert an image to the specified encoding format,
        /// using the default encoder properties from <see cref="ApplyEncoderDefaults(IImageEncoder)"/>.
        /// </summary>
        public Image<Rgba32> ConvertToFormat(byte[] imageContent, IImagingDevice.ImageFormat imageFormat);

        /// <inheritdoc cref="ConvertToFormat(byte[], IImagingDevice.ImageFormat)"/>
        public Image<Rgba32> ConvertToFormat(Image<Rgba32> image, IImagingDevice.ImageFormat imageFormat);

        /// <inheritdoc cref="Image.Load(byte[])"/>
        Image<TPixel> LoadImage(byte[] imageContent);

        IImageEncoder GetImageEncoder(IImagingDevice.ImageFormat imageFormat);
    }
}