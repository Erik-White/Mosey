using System.Collections.Generic;
using Mosey.Models.Imaging;

namespace Mosey.Services.Imaging
{
    public class ImageFileConfig : IImageFileConfig
    {
        public string Directory { get; set; }
        public string Prefix { get; set; }
        public string Format { get; set; }
        public List<string> SupportedFormats { get; set; }
        public string DateFormat { get; set; }
        public string TimeFormat { get; set; }

        public object Clone() => MemberwiseClone();
    }
}