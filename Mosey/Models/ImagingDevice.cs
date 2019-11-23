using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Mosey.Models
{
    public interface IImagingDevices<T> : IEnumerable<T>, IDisposable where T : IImagingDevice
    {
        bool DevicesReady { get; }
        void RefreshDevices();
        void RefreshDevices(IImagingDeviceSettings deviceSettings);
        void EnableDevice(T device);
        void EnableDevice(string deviceID);
        void EnableAll();
        void DisableAll();
        IEnumerable<T> GetByEnabled(bool enabled);
    }

    public interface IImagingDevice : IEquatable<IImagingDevice>, INotifyPropertyChanged, IDisposable
    {
        string Name { get; }
        int ID { get; }
        string DeviceID { get; }
        bool Enabled { get; set; }
        bool Connected { get; }
        bool Ready { get; }
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
