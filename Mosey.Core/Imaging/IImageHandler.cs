using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Core.Imaging
{
    public interface IImageHandler<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        /// <summary>
        /// Convert an image to the specified encoding format,
        /// using the default encoder properties from <see cref="ApplyEncoderDefaults(IImageEncoder)"/>.
        /// </summary>
        public Image<TPixel> ConvertToFormat(byte[] imageContent, IImagingDevice.ImageFormat imageFormat);

        /// <inheritdoc cref="ConvertToFormat(byte[], IImagingDevice.ImageFormat)"/>
        public Image<TPixel> ConvertToFormat(Image<TPixel> image, IImagingDevice.ImageFormat imageFormat);

        /// <inheritdoc cref="Image.Load(byte[])"/>
        Image<TPixel> LoadImage(byte[] imageContent);

        byte[] GetImageBytes(Image<TPixel> image);

        IImageEncoder GetImageEncoder(IImagingDevice.ImageFormat imageFormat);

        IImageEncoder GetImageEncoderWithDefaults(IImagingDevice.ImageFormat imageFormat);
    }
}