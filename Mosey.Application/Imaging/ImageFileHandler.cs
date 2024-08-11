using System.IO.Abstractions;
using Mosey.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Application.Imaging
{
    public class ImageFileHandler : IImageFileHandler
    {
        public readonly IImageHandler<Rgba32> _imageHandler;
        public readonly IFileSystem _fileSystem;

        public ImageFileHandler(IImageHandler<Rgba32> imageHandler, IFileSystem fileSystem)
        {
            _imageHandler = imageHandler;
            _fileSystem = fileSystem;
        }

        public void SaveImage(byte[] imageContent, IImagingDevice.ImageFormat imageFormat, string filePath)
            => SaveImage(_imageHandler.LoadImage(imageContent), imageFormat, filePath);

        /// <inheritdoc cref="SaveImage(byte[], IImagingDevice.ImageFormat, string)"/>
        public void SaveImage(Image image, IImagingDevice.ImageFormat imageFormat, string filePath)
        {
            var encoder = _imageHandler.GetImageEncoderWithDefaults(imageFormat);

            SaveImage(image, encoder, filePath);
        }


        /// <summary>
        /// Save an image to disk using the specified encoder.
        /// </summary>
        public void SaveImage(Image image, SixLabors.ImageSharp.Formats.IImageEncoder encoder, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be empty");
            }

            using (var fileStream = _fileSystem.FileStream.New(filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                image.Save(fileStream, encoder);
            }
        }
    }
}
