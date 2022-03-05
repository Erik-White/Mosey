using System.Collections.Generic;

namespace Mosey.Models
{
    /// <summary>
    /// A collection of <see cref="IDevice"/>s representing physical devices.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDeviceCollection<out T> where T : IDevice
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
        /// Update the status of exisiting <see cref="IDevice"/>s in the <see cref="Devices"/> collection.
        /// Any new <see cref="IDevice"/>s will be added to the collection.
        /// </summary>
        void RefreshDevices();
    }
}
