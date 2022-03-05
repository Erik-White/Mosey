using System.Collections.Generic;

namespace Mosey.Models.Imaging
{
    public record ImageFileConfig(
        string Directory,
        string Prefix,
        string Format,
        List<string> SupportedFormats,
        string DateFormat,
        string TimeFormat)
    {
        // Parameterless constructor required for use with WriteableOptions
        public ImageFileConfig()
            : this(default, default, default, new List<string>(), default, default)
        {
        }
    }
}