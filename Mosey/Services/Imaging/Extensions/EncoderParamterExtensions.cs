using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace Mosey.Services.Imaging.Extensions
{
    internal static class EncoderParamterExtensions
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
            var paramIndex = value.FindIndex(e => e is not null && e.Encoder == encoderParameter.Encoder);
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
        /// Add optional <see cref="EncoderParameter"/>s to a <see cref="EncoderParameters"/> collection.
        /// </summary>
        /// <param name="value">An <see cref="EncoderParameters"/> collection</param>
        /// <param name="compression">The algorithm used for compressing the image</param>
        /// <param name="quality">Expressed as a percentage, with 100 being original image quality</param>
        /// <param name="colorDepth">Colour range in bits per pixel</param>
        /// <param name="transform">A TransformFlip or TransformRotate <see cref="EncoderValue"/></param>
        /// <param name="frame">Specifies that the image has more than one frame (page). Can be passed to the TIFF encoder</param>
        /// <returns>A <see cref="EncoderParameters"/> collection containing the specified <see cref="EncoderParameter"/>s</returns>
        public static EncoderParameters AddParams(this EncoderParameters value, EncoderValue? compression = null, long? quality = null, long? colorDepth = null, EncoderValue? transform = null, EncoderValue? frame = null)
        {
            if (quality is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), quality.Value, $"{nameof(quality)} is a percentage and must be between 0 and 100");
            }

            if (colorDepth is <= 0 or > 48)
            {
                throw new ArgumentOutOfRangeException(nameof(colorDepth), colorDepth.Value, $"{nameof(colorDepth)} out of range");
            }

            // Replace existing values in the paramter list if they already exist
            // Otherwise just add them to the collection
            var encoderParams = value.Param.Where(e => e is not null).ToList();
            if (compression is not null)
            {
                encoderParams.AddOrUpdate(new EncoderParameter(Encoder.Compression, (long)compression));
            }

            if (quality is not null)
            {
                encoderParams.AddOrUpdate(new EncoderParameter(Encoder.Quality, (long)quality));
            }

            if (colorDepth is not null)
            {
                encoderParams.AddOrUpdate(new EncoderParameter(Encoder.ColorDepth, (long)colorDepth));
            }

            if (transform is not null)
            {
                encoderParams.AddOrUpdate(new EncoderParameter(Encoder.Transformation, (long)transform));
            }

            if (frame is not null)
            {
                encoderParams.AddOrUpdate(new EncoderParameter(Encoder.SaveFlag, (long)frame));
            }

            // Unfortunately the parameters can't be appended to the original collection directly
            // Create a new collection now that we know the total array size
            var paramCollection = new EncoderParameters(encoderParams.Count);
            for (var i = 0; i < encoderParams.Count; i++)
            {
                paramCollection.Param[i] = encoderParams[i];
            }

            // Clear up the original collection
            value?.Dispose();

            return paramCollection;
        }
    }
}
