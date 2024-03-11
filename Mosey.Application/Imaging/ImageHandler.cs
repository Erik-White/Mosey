using Mosey.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Application.Imaging
{
    public class ImageHandler : IImageHandler<Rgba32>
    {
        public Image<Rgba32> ConvertToFormat(byte[] imageContent, IImagingDevice.ImageFormat imageFormat)
        {
            using (var image = Image.Load<Rgba32>(imageContent))
            {
                return ConvertToFormat(image, imageFormat);
            }
        }

        public Image<Rgba32> ConvertToFormat(Image<Rgba32> image, IImagingDevice.ImageFormat imageFormat)
        {
            using (var stream = new MemoryStream())
            {
                var encoder = GetImageEncoder(imageFormat);

                image.Save(stream, encoder);
                stream.Seek(0,SeekOrigin.Begin);

                return Image.Load<Rgba32>(stream);
            }
        }

        public Image<Rgba32> LoadImage(byte[] imageContent)
            => Image.Load<Rgba32>(imageContent);

        public byte[] GetImageBytes(Image<Rgba32> image)
        {
            var format = image.Configuration.ImageFormats.FirstOrDefault();
            if (format is null)
            {
                throw new InvalidOperationException("Unable to detect image format.");
            }

            using var stream = new MemoryStream();
            image.Save(stream, format);

            return stream.ToArray();
        }

        public IImageEncoder GetImageEncoder(IImagingDevice.ImageFormat imageFormat)
            => imageFormat switch
            {
                IImagingDevice.ImageFormat.Bmp => new BmpEncoder
                {
                    BitsPerPixel = BmpBitsPerPixel.Pixel32,
                    SupportTransparency = false
                },
                IImagingDevice.ImageFormat.Gif => new GifEncoder(),
                IImagingDevice.ImageFormat.Jpeg => new JpegEncoder
                {
                    ColorType = JpegEncodingColor.Rgb,
                    Quality = 100
                },
                IImagingDevice.ImageFormat.Png => new PngEncoder
                {
                    BitDepth  = PngBitDepth.Bit16,
                    ColorType = PngColorType.Rgb,
                    CompressionLevel = PngCompressionLevel.NoCompression,
                    TransparentColorMode = PngTransparentColorMode.Clear
                },
                IImagingDevice.ImageFormat.Tiff => new TiffEncoder
                {
                    CompressionLevel = SixLabors.ImageSharp.Compression.Zlib.DeflateCompressionLevel.NoCompression,
                    BitsPerPixel = TiffBitsPerPixel.Bit24
                },
                _ => throw new NotSupportedException()
            };
    }
}
