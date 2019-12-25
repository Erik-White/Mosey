using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mosey.Models
{
    public interface IImagingDevices<T> : IDeviceCollection<T> where T : IImagingDevice
    {
        bool IsEmpty { get; }
        bool IsImagingInProgress { get; }
        void RefreshDevices(IImagingDeviceSettings deviceSettings, bool enableDevices);
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

    public interface IImagingDeviceSettings
    {
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }
}
