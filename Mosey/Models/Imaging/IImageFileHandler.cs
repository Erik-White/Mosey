namespace Mosey.Models.Imaging
{
    public interface IImageFileHandler
    {
        void SaveImage(byte[] imageContent, IImagingDevice.ImageFormat imageFormat, string filePath);
    }
}
