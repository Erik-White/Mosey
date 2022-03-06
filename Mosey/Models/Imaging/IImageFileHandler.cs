using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mosey.Models.Imaging
{
    public interface IImageFileHandler
    {
        void SaveImage(byte[] image, IImagingDevice.ImageFormat imageFormat, string filePath);
    }
}
