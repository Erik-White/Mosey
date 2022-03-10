using System;
using System.IO.Abstractions;
using Mosey.Models.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Services.Imaging
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

        public void SaveImage(Image image, IImagingDevice.ImageFormat imageFormat, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be empty");
            }

            using (var fileStream = _fileSystem.FileStream.Create(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write))
            {
                var encoder = _imageHandler.GetImageEncoder(imageFormat);
                _imageHandler.ApplyEncoderDefaults(encoder);

                image.Save(fileStream, encoder);
            }
        }
    }
}
