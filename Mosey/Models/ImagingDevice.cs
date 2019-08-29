using System;
using System.Collections.Generic;

namespace Mosey.Models
{
    public interface IImagingDevices : IEnumerable<IImagingDevice>
    {
        public void RefreshDevices() { }
        public void EnableDevice(IImagingDevice device) { }
        public void EnableDevice(string deviceName) { }
        public void EnableAll() { }
        public void DisableAll() { }

    }

    public interface IImagingDevice// : IDisposable
    {
        public string Name { get; }
        public string ID { get; }
        public bool Enabled { get; set; }
        public void GetImage() { }
        public void SaveImage() { }
        public void SaveImage(string directory, string fileName) { }
    }

    public interface IImagingDeviceSettings
    {
        public int Resolution { get; set; }
        public int Brightness { get; set; }
        public int Contrast { get; set; }
    }
}
