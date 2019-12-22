using System.Collections.Generic;

namespace Mosey.Models
{
    public interface IDeviceCollection<T> : IEnumerable<T> where T : IDevice
    {
        void EnableAll();
        void DisableAll();
        void SetDeviceEnabled(T device, bool enabled);
        void SetDeviceEnabled(string deviceID, bool enabled);
        IEnumerable<T> GetByEnabled(bool enabled);
        void RefreshDevices();
    }

    public interface IDevice
    {
        string Name { get; }
        int ID { get; }
        string DeviceID { get; }
        bool IsConnected { get; }
        bool IsEnabled { get; set; }
    }
}
