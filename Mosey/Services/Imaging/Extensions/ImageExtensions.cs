using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Mosey.Services.Imaging.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="Image"/> instances and related classes.
    /// </summary>
    public static class ImageExtensions
    {
        /// <summary>
        /// Convert an <see cref="Image"/> instance to a specified format and encoding.
        /// </summary>
        /// <param name="value">An <see cref="Image"/> instance</param>
        /// <param name="format">The format for the image</param>
        /// <param name="encoderParameters">A collection of <see cref="EncoderParameter"/>s to set image quality, colour depth etc</param>
        /// <returns>An <see cref="Image"/> instance in the specified format and encoding</returns>
        public static Image AsFormat(this Image value, ImageFormat format, EncoderParameters encoderParameters = null)
        {
            using (System.IO.MemoryStream ms = value.ToMemoryStream(format, encoderParameters))
            {
                return Image.FromStream(ms);
            }
        }

        /// <summary>
        /// Create a <see cref="byte"/> array from an <see cref="Image"/> instance.
        /// </summary>
        /// <param name="value">An <see cref="Image"/> instance</param>
        /// <returns>A <see cref="byte"/> array</returns>
        public static byte[] ToBytes(this Image value) => value.ToBytes(value.RawFormat);

        /// <summary>
        /// Create a <see cref="byte"/> array from an <see cref="Image"/> instance.
        /// </summary>
        /// <param name="value">An <see cref="Image"/> instance</param>
        /// <param name="format">The image format</param>
        /// <returns>An image <see cref="byte"/> array in the specified format</returns>
        public static byte[] ToBytes(this Image value, ImageFormat format) => value.ToBytes(format);

        /// <summary>
        /// Create a <see cref="byte"/> array from an <see cref="Image"/> instance.
        /// </summary>
        /// <param name="value">An <see cref="Image"/> instance</param>
        /// <param name="format">The image format</param>
        /// <param name="encoderParameters">A collection of <see cref="EncoderParameter"/>s to set image quality, colour depth etc</param>
        /// <returns>An image <see cref="byte"/> array in the specified format and encoding</returns>
        public static byte[] ToBytes(this Image value, ImageFormat format, EncoderParameters encoderParameters = null)
        {
            using (System.IO.MemoryStream ms = value.ToMemoryStream(format, encoderParameters))
            {
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Create a <see cref="MemoryStream"/> from an <see cref="Image"/> instance.
        /// The <see cref="Image"/> is converted to the specified format and encoding.
        /// </summary>
        /// <param name="value">An <see cref="Image"/> instance</param>
        /// <param name="format">The format for the image</param>
        /// <param name="encoderParameters">A collection of <see cref="EncoderParameter"/>s to set image quality, colour depth etc</param>
        /// <returns>An <see cref="Image"/> <see cref="MemoryStream"/> in the specified format and encoding</returns>
        internal static MemoryStream ToMemoryStream(this Image value, ImageFormat format, EncoderParameters encoderParameters = null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                value.Save(ms, format.CodecInfo(), encoderParameters);

                return ms;
            }
        }

        /// <summary>
        /// Find the <see cref="ImageCodecInfo"/> for an <see cref="ImageFormat"/> instance.
        /// </summary>
        /// <param name="value">An image format instance</param>
        /// <returns>An <see cref="ImageCodecInfo"/> instance corresponding to the <see cref="ImageFormat"/></returns>
        public static ImageCodecInfo CodecInfo(this ImageFormat value) => ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == value.Guid);
    }
}
