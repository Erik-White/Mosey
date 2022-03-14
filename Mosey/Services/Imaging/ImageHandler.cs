using System;
using Mosey.Models.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Services.Imaging
{
    public class ImageHandler : IImageHandler<Rgba32>
    {
        public void ApplyEncoderDefaults(IImageEncoder encoder)
        {
            switch (encoder)
            {
                case BmpEncoder bmpEncoder:
                    bmpEncoder.BitsPerPixel = BmpBitsPerPixel.Pixel32;
                    bmpEncoder.SupportTransparency = false;
                    break;

                case GifEncoder:
                    // Use the ImageSharp defaults
                    break;

                case JpegEncoder jpegEncoder:
                    jpegEncoder.ColorType = JpegColorType.Rgb;
                    jpegEncoder.Quality = 100;
                    break;

                case PngEncoder pngEncoder:
                    pngEncoder.BitDepth = PngBitDepth.Bit16;
                    pngEncoder.ColorType = PngColorType.Rgb;
                    pngEncoder.CompressionLevel = PngCompressionLevel.NoCompression;
                    pngEncoder.TransparentColorMode = PngTransparentColorMode.Clear;
                    break;

                case TiffEncoder tiffEncoder:
                    tiffEncoder.CompressionLevel = SixLabors.ImageSharp.Compression.Zlib.DeflateCompressionLevel.NoCompression;
                    tiffEncoder.BitsPerPixel = TiffBitsPerPixel.Bit24;
                    break;

                default:
                    throw new NotSupportedException("Unable to provide any default values for this encoder.");
            }
        }

        public Image<Rgba32> ConvertToFormat(byte[] imageContent, IImagingDevice.ImageFormat imageFormat)
        {
            using (var image = Image.Load<Rgba32>(imageContent))
            {
                return ConvertToFormat(image, imageFormat);
            }
        }

        public Image<Rgba32> ConvertToFormat(Image<Rgba32> image, IImagingDevice.ImageFormat imageFormat)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var encoder = GetImageEncoder(imageFormat);
                ApplyEncoderDefaults(encoder);

                image.Save(stream, encoder);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                return Image.Load<Rgba32>(stream);
            }
        }

        public Image<Rgba32> LoadImage(byte[] imageContent)
            => Image.Load<Rgba32>(imageContent);

        public IImageEncoder GetImageEncoder(IImagingDevice.ImageFormat imageFormat)
            => imageFormat switch
            {
                IImagingDevice.ImageFormat.Bmp => new BmpEncoder(),
                IImagingDevice.ImageFormat.Gif => new GifEncoder(),
                IImagingDevice.ImageFormat.Jpeg => new JpegEncoder(),
                IImagingDevice.ImageFormat.Png => new PngEncoder(),
                IImagingDevice.ImageFormat.Tiff => new TiffEncoder(),
                _ => throw new NotSupportedException()
            };
    }
}
