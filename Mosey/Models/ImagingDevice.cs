using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mosey.Models
{
    public interface IImagingDevices : IEnumerable<IImagingDevice>
    {
        bool DevicesReady { get; }
        void RefreshDevices();
        void RefreshDevices(IImagingDeviceSettings deviceSettings);
        void EnableDevice(IImagingDevice device);
        void EnableDevice(string deviceID);
        void EnableAll();
        void DisableAll();
        IEnumerable<IImagingDevice> GetByEnabled(bool enabled);
    }

    public interface IImagingDevice : IDisposable
    {
        string Name { get; }
        int ID { get; }
        string DeviceID { get; }
        bool Enabled { get; set; }
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
