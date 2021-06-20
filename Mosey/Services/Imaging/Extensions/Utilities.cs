using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Mosey.Services.Imaging.Extensions
{
    internal static class Utilities
    {
        /// <summary>
        /// Add an <see cref="EncoderParameter"/> to a list, or update the existing value if already present
        /// </summary>
        /// <param name="value">A list of <see cref="EncoderParameter"/>s</param>
        /// <param name="encoderParameter">An <see cref="EncoderParameter"/> instance to add or update</param>
        /// <returns>A list where the <see cref="EncoderParameter"/> has either replaced an existing value or been appended</returns>
        internal static List<EncoderParameter> AddOrUpdate(this List<EncoderParameter> value, EncoderParameter encoderParameter)
        {
            // Check if the encoder is already present in the list
            var paramIndex = value.FindIndex(e => e != null && e.Encoder == encoderParameter.Encoder);
            if (paramIndex >= 0)
            {
                // Update the existing value
                value[paramIndex]?.Dispose();
                value[paramIndex] = encoderParameter;
            }
            else
            {
                // Otherwise add it to the list
                value.Add(encoderParameter);
            }

            return value;
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
    }
}
