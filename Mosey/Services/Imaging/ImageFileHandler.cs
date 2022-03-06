using System;
using System.Drawing.Imaging;
using System.IO.Abstractions;
using Mosey.Models.Imaging;
using Mosey.Services.Imaging.Extensions;

namespace Mosey.Services.Imaging
{
    public class ImageFileHandler : IImageFileHandler
    {
        public readonly IFileSystem _fileSystem;

        public ImageFileHandler(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void SaveImage(byte[] image, IImagingDevice.ImageFormat imageFormat, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must not be empty");
            }

            // Use lossless compression with highest quality
            using (var encoderParams = new EncoderParameters().AddParams(
                compression: EncoderValue.CompressionLZW,
                quality: 100,
                colorDepth: 24))
            {
                using (var fileStream = _fileSystem.File.Create(filePath))
                {
                    image.ToImage().Save(
                        fileStream,
                        imageFormat.ToDrawingImageFormat().CodecInfo(),
                        encoderParams);
                }
            }
        }
    }
}
