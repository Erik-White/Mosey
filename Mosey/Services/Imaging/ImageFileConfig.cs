using System.Collections.Generic;
using Mosey.Models.Imaging;

namespace Mosey.Services.Imaging
{
    public record ImageFileConfig : IImageFileConfig
    {
        public string Directory { get; set; }
        public string Prefix { get; set; }
        public string Format { get; set; }
        public List<string> SupportedFormats { get; set; }
        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }
    }
}