using System.Collections.Generic;

namespace Mosey.Models.Imaging
{
    public record ImageFileConfig(
        string Directory,
        string Prefix,
        IImagingDevice.ImageFormat ImageFormat,
        List<IImagingDevice.ImageFormat> SupportedFormats,
        string DateFormat,
        string TimeFormat)
    {
        // Parameterless constructor required for use with WriteableOptions
        public ImageFileConfig()
            : this(default, default, default, default, default, default)
        {
        }
    }
}