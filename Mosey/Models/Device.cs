using System.Collections.Generic;

namespace Mosey.Models
{
    /// <summary>
    /// A collection of <see cref="IDevice"/>s representing physical devices.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDeviceCollection<T> where T : IDevice
    {
        /// <summary>
        /// A collection of <see cref="IDevice"/>s.
        /// </summary>
        IEnumerable<T> Devices { get; }

        /// <summary>
        /// Add a <see cref="IDevice"/> to the collection.
        /// </summary>
        /// <param name="device">A <see cref="IDevice"/> instance</param>
        void AddDevice(IDevice device);

        /// <summary>
        /// Set the <see cref="IDevice.IsEnabled"/> property of all devices in the collection to <see langword="true"/>.
        /// </summary>
        void EnableAll();

        /// <summary>
        /// Set the <see cref="IDevice.IsEnabled"/> property of all devices in the collection to <see langword="false"/>.
        /// </summary>
        void DisableAll();

        /// <summary>
        /// Set the <see cref="IDevice.IsEnabled"/> property of a device in the collection.
        /// </summary>
        /// <param name="deviceID">The unique device identifier</param>
        /// <param name="enabled">Sets the <see cref="IDevice.IsEnabled"/> property</param>
        void SetDeviceEnabled(string deviceID, bool enabled);

        /// <summary>
        /// Update the status of exisiting <see cref="IDevice"/>s in the <see cref="Devices"/> collection.
        /// Any new <see cref="IDevice"/>s will be added to the collection.
        /// </summary>
        void RefreshDevices();
    }

    /// <summary>
    /// Represents a physical device connected to the system.
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// A label to represent the device make or model.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// An internal identifier for the device.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// The unique indentifier of the physical device.
        /// </summary>
        string DeviceID { get; }

        /// <summary>
        /// If the physical device can be found by the system.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Sets whether the device is allowed to operate.
        /// </summary>
        bool IsEnabled { get; set; }
    }
}
