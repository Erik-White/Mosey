namespace Mosey.Core.Imaging
{
    public interface IImageFileHandler
    {
        /// <summary>
        /// Save an image to disk using the default encoder parameters.
        /// </summary>
        void SaveImage(byte[] imageContent, IImagingDevice.ImageFormat imageFormat, string filePath);
    }
}
