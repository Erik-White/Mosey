using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using Mosey.Core.Imaging;
using Mosey.Tests;
using Mosey.Tests.AutoData;
using NSubstitute;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Application.Imaging.Tests
{
    public class ImageFileHandlerTests
    {
        public class SaveImageShould
        {
            [Theory, AutoNSubstituteData]
            public void ThrowArgumentException_IfFilePathIsEmpty(ImageFileHandler sut)
            {
                Action act = () => sut.SaveImage(Array.Empty<byte>(), default, string.Empty);

                act.Should().Throw<ArgumentException>();
            }

            [Theory, AutoNSubstituteData]
            public void WriteExpectedData([Frozen] IFileSystem fileSystem, ImageHandler imageHandler, [Frozen] Image<Rgba32> image,MockFilePath filePath)
            {
                try
                {
                    var pngImage = imageHandler.ConvertToFormat(image, IImagingDevice.ImageFormat.Png);
                    var sut = new ImageFileHandler(imageHandler, fileSystem);

                    sut.SaveImage(image, IImagingDevice.ImageFormat.Png, filePath.Path);

                    using (new AssertionScope())
                    using (var fileStream = fileSystem.File.OpenRead((fileSystem as MockFileSystem).AllFiles.First()))
                    using (var savedImage = Image.Load<Rgba32>(fileStream))
                    {
                        savedImage.Should().NotBeSameAs(image);

                        for (int i = 0; i < savedImage.Height; i++)
                        {
                            var row = savedImage.Frames.RootFrame.PixelBuffer.DangerousGetRowSpan(i);
                            Assert.That(row.SequenceEqual(pngImage.Frames.RootFrame.PixelBuffer.DangerousGetRowSpan(i)));
                        }
                    }
                }
                finally
                {
                    image?.Dispose();
                }
            }

            [Theory, AutoNSubstituteData]
            public void WriteExpectedFormat([Frozen] IFileSystem fileSystem, [Frozen] Image<Rgba32> image, MockFilePath filePath)
            {
                try
                {
                    var sut = new ImageFileHandler(new ImageHandler(), fileSystem);

                    sut.SaveImage(image, IImagingDevice.ImageFormat.Png, filePath.Path);

                    using (new AssertionScope())
                    using (var fileStream = fileSystem.File.OpenRead((fileSystem as MockFileSystem).AllFiles.First()))
                    using (var savedImage = Image.Load(fileStream))
                    {
                        savedImage.Should().NotBeSameAs(image);
                        savedImage.Width.Should().Be(image.Width);
                        savedImage.Height.Should().Be(image.Height);
                        fileStream.Seek(0, System.IO.SeekOrigin.Begin);
                        Image.DetectFormat(fileStream).Should().BeOfType(typeof(PngFormat));
                    }
                }
                finally
                {
                    image?.Dispose();
                }
            }

            [Theory, AutoNSubstituteData]
            public void WriteExpectedPath([Frozen] IFileSystem fileSystem, ImageFileHandler sut, MockFilePath filePath)
            {
                sut.SaveImage(Array.Empty<byte>(), default, filePath.Path);

                (fileSystem as MockFileSystem).AllFiles
                    .Should().HaveCount(1)
                    .And.ContainSingle(x => x == filePath.Path);
            }
        }
    }
}
