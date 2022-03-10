using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using Mosey.Models.Imaging;
using Mosey.Tests;
using Mosey.Tests.AutoData;
using NSubstitute;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Mosey.Services.Imaging.Tests
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
            public void WriteExpectedPath([Frozen] IFileSystem fileSystem, ImageFileHandler sut, MockFilePath filePath)
            {
                sut.SaveImage(Array.Empty<byte>(), default, filePath.Path);

                (fileSystem as MockFileSystem).AllFiles
                    .Should().HaveCount(1)
                    .And.ContainSingle(x => x == filePath.Path);
            }

            [Theory, AutoNSubstituteData]
            public void WriteExpectedData([Frozen] IFileSystem fileSystem, [Frozen] IImageHandler<Rgba32> imageHandler, [Frozen] Image<Rgba32> image, MockFilePath filePath, ImageFileHandler sut)
            {
                try
                {
                    imageHandler.GetImageEncoder(default).ReturnsForAnyArgs(new PngEncoder());

                    sut.SaveImage(Array.Empty<byte>(), default, filePath.Path);

                    using (new AssertionScope())
                    using (var fileStream = fileSystem.File.OpenRead((fileSystem as MockFileSystem).AllFiles.First()))
                    using (var savedImage = Image.Load(fileStream, out var format))
                    {
                        savedImage.Should().NotBeSameAs(image);
                        savedImage.Width.Should().Be(image.Width);
                        savedImage.Height.Should().Be(image.Height);
                        format.Should().BeOfType(typeof(PngFormat));
                    }
                }
                finally
                {
                    image?.Dispose();
                }
            }
        }
    }
}
