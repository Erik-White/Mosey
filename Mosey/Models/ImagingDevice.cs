using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mosey.Models
{
    public interface IImagingDevices<T> : IEnumerable<T>, IDisposable where T : IImagingDevice
    {
        void RefreshDevices();
        void RefreshDevices(IImagingDeviceSettings deviceSettings);
        void SetDeviceEnabled(T device);
        void SetDeviceEnabled(string deviceID);
        void EnableAll();
        void DisableAll();
        IEnumerable<T> GetByEnabled(bool enabled);
    }

    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged, IDisposable
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
    }

    public interface IImagingDeviceSettings
    {
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }
}
