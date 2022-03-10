using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Models.Imaging
{
    public interface IImageHandler<TPixel> where TPixel : unmanaged, IPixel<TPixel>
    {
        void ApplyEncoderDefaults(IImageEncoder encoder);

        public Image<Rgba32> ConvertToFormat(byte[] imageContent, IImagingDevice.ImageFormat imageFormat);

        public Image<Rgba32> ConvertToFormat(Image<Rgba32> image, IImagingDevice.ImageFormat imageFormat);

        Image<TPixel> LoadImage(byte[] imageContent);

        IImageEncoder GetImageEncoder(IImagingDevice.ImageFormat imageFormat);
    }
}