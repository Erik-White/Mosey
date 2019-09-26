using System;
using System.Collections.Generic;

namespace Mosey.Models
{
    public interface IImagingDevices : IEnumerable<IImagingDevice>
    {
        void RefreshDevices();
        void RefreshDevices(IImagingDeviceSettings deviceSettings);
        void EnableDevice(IImagingDevice device);
        void EnableDevice(string deviceName);
        void EnableAll();
        void DisableAll();
        IEnumerable<IImagingDevice> GetByEnabled(bool enabled);

    }

    public interface IImagingDevice : IDisposable
    {
        string Name { get; }
        string ID { get; }
        bool Enabled { get; set; }
        void GetImage();
        void SaveImage();
        IEnumerable<string> SaveImage(string directory, string fileName);
    }

    public interface IImagingDeviceSettings
    {
        int Resolution { get; set; }
        int Brightness { get; set; }
        int Contrast { get; set; }
    }
}
