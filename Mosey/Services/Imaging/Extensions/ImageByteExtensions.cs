using System.Drawing;
using System.Drawing.Imaging;

namespace Mosey.Services.Imaging.Extensions
{
    /// <summary>
    /// Extension methods for images stored as <see cref="byte"/> arrays
    /// </summary>
    public static class ImageByteExtensions
    {
        /// <summary>
        /// Convert an image <see cref="byte"/> array to a specified format and encoding.
        /// </summary>
        /// <param name="value">An image as a byte array</param>
        /// <param name="format">The format for the image</param>
        /// <param name="encoderParameters">A collection of <see cref="EncoderParameter"/>s to set image quality, colour depth etc</param>
        /// <returns>An image <see cref="byte"/> array in the specified format and encoding</returns>
        public static byte[] AsFormat(this byte[] value, ImageFormat format, EncoderParameters encoderParameters = null)
        {
            return value.ToImage().ToBytes(format, encoderParameters);
        }

        /// <summary>
        /// Create an <see cref="Image"/> instance from an image <see cref="byte"/> array.
        /// </summary>
        /// <param name="value">An image as a <see cref="byte"/> array</param>
        /// <returns>An <see cref="Image"/> instance</returns>
        public static Image ToImage(this byte[] value)
        {
            var ms = new System.IO.MemoryStream(value);
            return Image.FromStream(ms);
        }

        /// <summary>
        /// Create an <see cref="Image"/> instance from an image <see cref="byte"/> array.
        /// </summary>
        /// <param name="value">An image as a <see cref="byte"/> array</param>
        /// <param name="format">The image format</param>
        /// <returns>An <see cref="Image"/> instance in the specified format</returns>
        public static Image ToImage(this byte[] value, ImageFormat format)
        {
            return value.ToImage(format);
        }

        /// <summary>
        /// Create an <see cref="Image"/> instance from an image <see cref="byte"/> array.
        /// </summary>
        /// <param name="value">An image as a byte array</param>
        /// <param name="format">The format for the image</param>
        /// <param name="encoderParameters">A collection of <see cref="EncoderParameter"/>s to set image quality, colour depth etc</param>
        /// <returns>An <see cref="Image"/> instance in the specified format and encoding</returns>
        public static Image ToImage(this byte[] value, ImageFormat format, EncoderParameters encoderParameters = null)
        {
            using (var ms = value.ToImage().ToMemoryStream(format, encoderParameters))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
