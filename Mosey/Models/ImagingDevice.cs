using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mosey.Models
{
    public interface IImagingDevices<T> : IDeviceCollection<T> where T : IImagingDevice
    {
        bool IsEmpty { get; }
        bool IsImagingInProgress { get; }
        void RefreshDevices(IImagingDeviceConfig deviceSettings, bool enableDevices);
    }

    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged, IDevice
    {
        IList<byte[]> Images { get; }
        bool IsImaging { get; }
        void ClearImages();
        void GetImage();
        void SaveImage();
        IEnumerable<string> SaveImage(string fileName, string directory, string fileFormat);
    }

    public enum ImageColorFormat
    {
        Color,
        BlackAndWhite,
        Greyscale
    }

    public interface IImagingDeviceConfig
    {
        ImageColorFormat ColorFormat { get; set; }
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }

    public interface IImageFileConfig
    {
        string Path { get; set; }
        string Prefix { get; set; }
        string Format { get; set; }
        List<string> SupportedFormats { get; set; }
        string DateFormat { get; set; }
        string TimeFormat { get; set; }
    }
}
