using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mosey.Models
{
    public interface IImagingDevices<T> : IEnumerable<T>, IDisposable where T : IImagingDevice
    {
        bool IsScanInProgress { get; }
        bool IsEmpty { get; }
        void RefreshDevices();
        void RefreshDevices(IImagingDeviceSettings deviceSettings, bool enableDevices);
        void SetDeviceEnabled(T device, bool enabled);
        void SetDeviceEnabled(string deviceID, bool enabled);
        void EnableAll();
        void DisableAll();
        IEnumerable<T> GetByEnabled(bool enabled);
    }

    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged
    {
        string Name { get; }
        int ID { get; }
        string DeviceID { get; }
        bool IsEnabled { get; set; }
        bool IsConnected { get; }
        bool IsScanning { get; }
        void GetImage();
        void SaveImage();
        IEnumerable<string> SaveImage(string fileName, string directory, string fileFormat);
        Exception ErrorState { get; }
    }

    public interface IImagingDeviceSettings
    {
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }
}
